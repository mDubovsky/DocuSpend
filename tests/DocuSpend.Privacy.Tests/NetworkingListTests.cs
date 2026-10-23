namespace DocuSpend.Privacy.Tests;

/// <summary>The lists of what counts as networking in shipped dependencies are read correctly.</summary>
[Trait("Check", "privacy-dependencies")]
public class NetworkingListTests
{
    private static readonly NetworkingList List = NetworkingList.Repository.Value;

    [Fact]
    public void The_System_Net_entries_of_the_ban_list_apply_to_dependencies()
    {
        Assert.Contains("System.Net.Http", List.Namespaces);
        Assert.Contains("System.Net.Sockets", List.Namespaces);
        Assert.Contains("System.Net.Dns", List.Types);
        Assert.Contains("System.Net.WebClient", List.Types);
    }

    [Fact]
    public void Reflection_and_starting_programs_are_banned_only_in_our_own_code()
    {
        Assert.DoesNotContain(List.Types, t => t.StartsWith("System.Diagnostics", StringComparison.Ordinal));
        Assert.DoesNotContain(List.Types, t => t.StartsWith("System.Reflection", StringComparison.Ordinal));
        Assert.DoesNotContain(List.Namespaces, n => !n.StartsWith("System.Net", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("System.Net.Http", "HttpClient", true)]
    [InlineData("System.Net.Http.Json", "JsonContent", true)]
    [InlineData("System.Net", "Dns", true)]
    [InlineData("System.Net", "IPAddress", false)]
    [InlineData("System.Net.Mime", "MediaTypeNames", false)]
    [InlineData("System.Net.HttpX", "Lookalike", false)]
    public void Networking_types_are_matched_by_namespace_or_full_name(string ns, string name, bool expected)
    {
        Assert.Equal(expected, List.IsNetworkingType(ns, name));
    }

    [Theory]
    [InlineData("ws2_32.dll", "ws2_32")]
    [InlineData("WS2_32", "ws2_32")]
    [InlineData(@"C:\Windows\System32\wininet.dll", "wininet")]
    [InlineData("/usr/lib/x86_64-linux-gnu/libcurl.so.4", "curl")]
    [InlineData("libcurl.so", "curl")]
    [InlineData("libcurl.4.dylib", "curl")]
    [InlineData("libssl.so.3", "ssl")]
    [InlineData("lib", "lib")]
    public void Library_names_are_normalized(string library, string expected)
    {
        Assert.Equal(expected, NetworkingList.NormalizeLibrary(library));
    }

    [Theory]
    [InlineData("ws2_32.dll", "anything", true)]
    [InlineData("libcurl.so.4", "curl_version", true)]
    [InlineData("libc", "getaddrinfo", true)]
    [InlineData("libc", "Connect", true)]
    [InlineData("kernel32.dll", "GetTickCount", false)]
    [InlineData("libc", "open", false)]
    public void Native_imports_are_matched_by_library_or_function(string library, string function, bool expected)
    {
        Assert.Equal(expected, List.IsNetworkingImport(library, function));
    }

    [Fact]
    public void A_malformed_native_networking_line_fails()
    {
        Assert.Throws<FormatException>(() => NetworkingList.Parse("", "library ws2_32\nws2_32"));
        Assert.Throws<FormatException>(() => NetworkingList.Parse("", "module ws2_32"));
    }
}
