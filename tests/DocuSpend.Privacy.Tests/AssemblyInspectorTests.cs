namespace DocuSpend.Privacy.Tests;

/// <summary>The metadata inspector finds networking in an assembly without loading it (plan: the dependency inspector).</summary>
[Trait("Check", "privacy-dependencies")]
public class AssemblyInspectorTests
{
    private static InspectedAssembly Inspect(string source) =>
        AssemblyInspector.Inspect(FixtureOutput.Compile("Fixture", source), NetworkingList.Repository.Value)!;

    [Fact]
    public void A_clean_assembly_has_no_networking()
    {
        var assembly = Inspect("public class Clean { public byte[] Read(string p) => System.IO.File.ReadAllBytes(p); }");

        Assert.Equal("Fixture", assembly.Name);
        Assert.Empty(assembly.NetworkingUses);
    }

    [Theory]
    [InlineData("public class N { public object M() => new System.Net.Http.HttpClient(); }", "System.Net.Http.HttpClient")]
    [InlineData("public class N { public object M() => new System.Net.Http.Headers.MediaTypeHeaderValue(\"a/b\"); }", "System.Net.Http.Headers.MediaTypeHeaderValue")]
    [InlineData("public class N { public object M() => new System.Net.Sockets.TcpClient(); }", "System.Net.Sockets.TcpClient")]
    [InlineData("public class N { public object M() => System.Net.Dns.GetHostName(); }", "System.Net.Dns")]
    [InlineData("public class N { public object M() => new System.Net.Mail.SmtpClient(); }", "System.Net.Mail.SmtpClient")]
    [InlineData("public class N { public object M() => System.Net.WebSockets.WebSocketState.Open; }", "System.Net.WebSockets.WebSocketState")]
    public void A_reference_to_a_networking_type_is_found(string source, string expected)
    {
        Assert.Contains(expected, Inspect(source).NetworkingUses);
    }

    [Fact]
    public void A_nested_networking_type_is_found_by_its_outer_type()
    {
        using var output = new FixtureOutput();
        output.Add("Net", "namespace System.Net.Http { public class Outer { public class Inner { } } }");
        var user = output.Add("User", "public class U { public object M() => new System.Net.Http.Outer.Inner(); }", references: ["Net"]);

        var uses = AssemblyInspector.Inspect(user, NetworkingList.Repository.Value)!.NetworkingUses;

        Assert.Equal("System.Net.Http.Outer", Assert.Single(uses));
    }

    [Fact]
    public void A_library_that_defines_its_own_networking_types_is_found()
    {
        var uses = Inspect("namespace System.Net.Http { public class HttpClient { } }").NetworkingUses;

        Assert.Contains("System.Net.Http.HttpClient", uses);
    }

    [Theory]
    [InlineData("[System.Runtime.InteropServices.DllImport(\"ws2_32.dll\")] static extern int closesocket(nint s);", "native ws2_32.dll!closesocket")]
    [InlineData("[System.Runtime.InteropServices.DllImport(\"WININET\", EntryPoint = \"InternetOpenW\")] static extern nint Open(string a, int b, string c, string d, int e);", "native WININET!InternetOpenW")]
    [InlineData("[System.Runtime.InteropServices.DllImport(\"libc\")] static extern int connect(int s, nint a, int l);", "native libc!connect")]
    [InlineData("[System.Runtime.InteropServices.DllImport(\"libcurl.so.4\")] static extern nint curl_version();", "native libcurl.so.4!curl_version")]
    public void A_native_networking_import_is_found(string declaration, string expected)
    {
        var uses = Inspect($"public static class Native {{ {declaration} }}").NetworkingUses;

        Assert.Equal(expected, Assert.Single(uses));
    }

    [Fact]
    public void Harmless_types_and_imports_are_not_findings()
    {
        const string source = """
            public static class Harmless
            {
                public static object Address() => System.Net.IPAddress.Loopback;
                public static string Encode() => System.Net.WebUtility.UrlEncode("a b");
                public static object Start() => System.Diagnostics.Process.GetCurrentProcess();
                [System.Runtime.InteropServices.DllImport("kernel32.dll")] public static extern uint GetTickCount();
            }
            """;

        Assert.Empty(Inspect(source).NetworkingUses);
    }

    [Fact]
    public void A_file_that_is_not_a_managed_assembly_is_not_inspected()
    {
        Assert.Null(AssemblyInspector.Inspect([0x7F, (byte)'E', (byte)'L', (byte)'F', 1, 2, 3], NetworkingList.Repository.Value));
        Assert.Null(AssemblyInspector.Inspect([], NetworkingList.Repository.Value));
    }
}
