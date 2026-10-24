using System.Text.RegularExpressions;

namespace DocuSpend.Privacy.Tests;

/// <summary>
/// What counts as networking in an assembly we ship but didn't write: the System.Net namespaces and types of
/// privacy/BannedSymbols.txt, and the native libraries and functions of privacy/native-networking.txt.
/// Reflection, Process and the rest of the ban list apply only to our own code, because libraries legitimately
/// use them.
/// </summary>
public sealed partial class NetworkingList
{
    public IReadOnlyList<string> Namespaces { get; }
    public IReadOnlySet<string> Types { get; }
    public IReadOnlySet<string> Libraries { get; }
    public IReadOnlySet<string> Functions { get; }

    public NetworkingList(IEnumerable<string> namespaces, IEnumerable<string> types,
        IEnumerable<string> libraries, IEnumerable<string> functions)
    {
        Namespaces = namespaces.ToList();
        Types = types.ToHashSet(StringComparer.Ordinal);
        Libraries = libraries.Select(NormalizeLibrary).ToHashSet(StringComparer.Ordinal);
        Functions = functions.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static readonly Lazy<NetworkingList> Repository = new(() => Parse(
        File.ReadAllText(Path.Combine(Repo.PrivacyDir, "BannedSymbols.txt")),
        File.ReadAllText(Path.Combine(Repo.PrivacyDir, "native-networking.txt"))));

    public static NetworkingList Parse(string bannedSymbols, string nativeNetworking)
    {
        var ids = Lines(bannedSymbols, "//").Select(line => line.Split(';', 2)[0]).ToList();
        var native = Lines(nativeNetworking, "#").Select(line => line.Split(' ', 2, StringSplitOptions.TrimEntries)).ToList();
        if (native.FirstOrDefault(parts => parts.Length != 2 || parts[0] is not ("library" or "function")) is { } bad)
            throw new FormatException($"privacy/native-networking.txt: expected 'library <name>' or 'function <name>', got '{string.Join(' ', bad)}'.");

        return new NetworkingList(
            ids.Where(id => id.StartsWith("N:System.Net", StringComparison.Ordinal)).Select(id => id[2..]),
            ids.Where(id => id.StartsWith("T:System.Net.", StringComparison.Ordinal)).Select(id => id[2..]),
            native.Where(p => p[0] == "library").Select(p => p[1]),
            native.Where(p => p[0] == "function").Select(p => p[1]));
    }

    /// <summary>Whether a type, by namespace and outermost type name, is networking.</summary>
    public bool IsNetworkingType(string ns, string name) =>
        Types.Contains($"{ns}.{name}")
        || Namespaces.Any(n => ns == n || ns.StartsWith(n + ".", StringComparison.Ordinal));

    /// <summary>Whether a native import, by library and function name, is networking.</summary>
    public bool IsNetworkingImport(string library, string function) =>
        Libraries.Contains(NormalizeLibrary(library)) || Functions.Contains(function);

    /// <summary>"Ws2_32.dll", "ws2_32", "/usr/lib/libcurl.so.4" and "curl" all become comparable.</summary>
    public static string NormalizeLibrary(string library)
    {
        var name = library.Replace('\\', '/');
        name = name[(name.LastIndexOf('/') + 1)..].ToLowerInvariant();
        var so = name.IndexOf(".so", StringComparison.Ordinal);
        if (so > 0 && (so + 3 == name.Length || name[so + 3] == '.'))
            name = name[..so];
        foreach (var extension in new[] { ".dll", ".dylib", ".exe" })
        {
            if (name.EndsWith(extension, StringComparison.Ordinal))
                name = name[..^extension.Length];
        }
        name = TrailingVersion().Replace(name, "");
        return name.StartsWith("lib", StringComparison.Ordinal) && name.Length > 3 ? name[3..] : name;
    }

    /// <summary>A version after the name, as in libcurl.4.dylib.</summary>
    [GeneratedRegex(@"(\.\d+)+$")]
    private static partial Regex TrailingVersion();

    private static IEnumerable<string> Lines(string text, string comment) => text.Split('\n')
        .Select(line => line.Trim())
        .Where(line => line.Length > 0 && !line.StartsWith(comment, StringComparison.Ordinal));
}
