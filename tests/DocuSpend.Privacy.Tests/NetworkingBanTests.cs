using Microsoft.CodeAnalysis;

namespace DocuSpend.Privacy.Tests;

/// <summary>
/// The ban on networking in our own code catches each kind of violation, in each kind of project, with a message
/// that names the rule, decision 0006 and how to request an exception (spec: privacy-no-networking-code).
/// </summary>
[Trait("Check", "privacy-no-networking-code")]
public class NetworkingBanTests
{
    public static TheoryData<string, string> Violations => new()
    {
        { "HTTP", "object M() => new System.Net.Http.HttpClient();" },
        { "HTTP, nested namespace", "object M() => new System.Net.Http.Headers.MediaTypeHeaderValue(\"text/plain\");" },
        { "socket", "object M() => new System.Net.Sockets.Socket(System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);" },
        { "TCP client", "object M() => new System.Net.Sockets.TcpClient(\"example.com\", 80);" },
        { "web socket", "object M() => new System.Net.WebSockets.ClientWebSocket();" },
        { "QUIC", "bool M() => System.Net.Quic.QuicConnection.IsSupported;" },
        { "mail", "object M() => new System.Net.Mail.SmtpClient();" },
        { "DNS", "object M() => System.Net.Dns.GetHostAddresses(\"example.com\");" },
        { "ping", "object M() => new System.Net.NetworkInformation.Ping();" },
        { "secure stream", "object M() => new System.Net.Security.SslStream(System.IO.Stream.Null);" },
        { "web client", "object M() => new System.Net.WebClient();" },
        { "web request", "object M() => System.Net.WebRequest.Create(\"https://example.com\");" },
        { "HTTP listener", "object M() => new System.Net.HttpListener();" },
        { "named pipe", "object M() => new System.IO.Pipes.NamedPipeClientStream(\"server\", \"pipe\");" },
        { "starting a program", "object? M() => System.Diagnostics.Process.Start(\"curl\", \"https://example.com\");" },
        { "start info", "object M() => new System.Diagnostics.ProcessStartInfo(\"curl\");" },
        { "DllImport", "[System.Runtime.InteropServices.DllImport(\"ws2_32.dll\")] static extern int connect(nint s, nint name, int len);" },
        { "LibraryImport", "[System.Runtime.InteropServices.LibraryImport(\"libc\")] private static partial int connect(int s, nint name, int len);" },
        { "native library", "nint M() => System.Runtime.InteropServices.NativeLibrary.Load(\"libcurl\");" },
        { "COM class", "[System.Runtime.InteropServices.ComImport, System.Runtime.InteropServices.Guid(\"2087C2F4-2CEF-4953-A8AB-66779B670495\")] class WinHttp { }" },
        { "COM by name", "System.Type? M() => System.Type.GetTypeFromProgID(\"WinHttp.WinHttpRequest.5.1\");" },
        { "type by name", "System.Type? M() => System.Type.GetType(\"System.Net.Http.HttpClient, System.Net.Http\");" },
        { "type by name from an assembly", "System.Type? M() => typeof(object).Assembly.GetType(\"System.Net.Http.HttpClient\");" },
        { "assembly by name", "object M() => System.Reflection.Assembly.Load(\"System.Net.Http\");" },
        { "assembly from a file", "object M() => System.Reflection.Assembly.LoadFrom(\"net.dll\");" },
        { "assembly load context", "object M() => System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(\"/net.dll\");" },
        { "instance by name", "object? M() => System.Activator.CreateInstance(\"System.Net.Http\", \"System.Net.Http.HttpClient\");" },
    };

    [Theory]
    [MemberData(nameof(Violations))]
    public void A_violation_fails_with_a_message_naming_the_rule_the_decision_and_the_way_to_an_exception(string kind, string member)
    {
        var violations = BanCompiler.Violations(InClass(member));

        Assert.True(violations.Count > 0, $"The ban did not catch: {kind}");
        foreach (var message in violations.Select(v => v.GetMessage()))
        {
            Assert.Contains("privacy-no-networking-code", message);
            Assert.Contains("decision 0006", message);
            Assert.Contains("docs/decisions/0006-privacy-approach-and-network-boundary.md", message);
            Assert.Contains("To request an exception, see privacy/README.md", message);
        }
    }

    [Fact]
    public void A_violation_names_its_location()
    {
        var violation = Assert.Single(BanCompiler.Violations(InClass("object M() => new System.Net.Http.HttpClient();")));

        var position = violation.Location.GetLineSpan();
        Assert.Equal("Snippet.cs", position.Path);
        Assert.Equal(0, position.StartLinePosition.Line);
    }

    [Fact]
    public void Harmless_neighbours_of_banned_code_are_allowed()
    {
        const string members = """
            object Address() => System.Net.IPAddress.Loopback;
            string Encode() => System.Net.WebUtility.UrlEncode("a b");
            object Link() => new System.Uri("https://example.com");
            System.Type OwnType(object o) => o.GetType();
            object? Instance() => System.Activator.CreateInstance(typeof(object));
            byte[] Read(string path) => System.IO.File.ReadAllBytes(path);
            object Types() => typeof(int).Assembly.GetTypes();
            """;

        Assert.Empty(BanCompiler.Violations(InClass(members)));
    }

    [Fact]
    public void The_ban_applies_to_an_application_library()
    {
        Assert.NotEmpty(BanCompiler.Violations(InClass("object M() => new System.Net.Http.HttpClient();")));
    }

    [Fact]
    public void The_ban_applies_to_a_command_line_tool()
    {
        const string tool = """
            internal static class Program
            {
                private static void Main() => new System.Net.Http.HttpClient().GetStringAsync("https://example.com").Wait();
            }
            """;

        Assert.NotEmpty(BanCompiler.Violations(tool, OutputKind.ConsoleApplication));
    }

    [Fact]
    public void The_ban_applies_to_a_test()
    {
        const string test = """
            public class SomeTests
            {
                [Xunit.Fact]
                public void Downloads() => new System.Net.Http.HttpClient().GetStringAsync("https://example.com").Wait();
            }
            """;

        var xunit = new[] { typeof(FactAttribute).Assembly.Location, typeof(Assert).Assembly.Location };
        Assert.NotEmpty(BanCompiler.Violations(test, extraReferences: xunit));
    }

    [Fact]
    public void The_list_must_be_named_exactly_BannedSymbols_txt_or_the_analyzer_ignores_it()
    {
        // Why the name is guarded (see BanListTests): under any other name the ban silently stops working.
        const string member = "object M() => new System.Net.Http.HttpClient();";

        Assert.NotEmpty(BanCompiler.Violations(InClass(member), banFileName: "BannedSymbols.txt"));
        Assert.Empty(BanCompiler.Violations(InClass(member), banFileName: "NetworkingBan.txt"));
    }

    [Fact]
    public void Avalonias_previewer_transport_is_banned_in_our_code()
    {
        // Its networking is allowed in what ships only because our code can never reach it (allowed-dependencies.json).
        string[] avalonia = [Path.Combine(ShippedDependenciesTests.OutputDirOf("src/DocuSpend.Desktop/DocuSpend.Desktop.csproj"), "Avalonia.Remote.Protocol.dll")];

        var violations = BanCompiler.Violations(InClass("object M() => new Avalonia.Remote.Protocol.BsonTcpTransport();"), extraReferences: avalonia);

        Assert.Contains(violations, v => v.GetMessage().Contains("Avalonia's previewer protocol", StringComparison.Ordinal));
    }

    private static string InClass(string members) => $"partial class C {{ {members}\n}}";
}
