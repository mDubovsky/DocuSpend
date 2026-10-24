using System.Text.Json;
using System.Text.RegularExpressions;

namespace DocuSpend.Privacy.Tests;

/// <summary>A package a project resolves, and the chain of dependencies from the project that brings it in.</summary>
public sealed record ResolvedPackage(string Id, string Version, IReadOnlyList<string> Path)
{
    public string PathText => string.Join(" → ", Path);
}

/// <summary>
/// Reads a project's obj/project.assets.json, the record NuGet writes of every package the project resolves,
/// directly or indirectly, and of the files each one contributes to the build output.
/// </summary>
public static class PackageAssets
{
    /// <summary>Every package the project resolves, with the shortest dependency path from the project to it.</summary>
    public static IReadOnlyList<ResolvedPackage> Packages(string projectName, string assetsJson)
    {
        using var doc = JsonDocument.Parse(assetsJson);
        var root = doc.RootElement;
        var libraries = Targets(root).SelectMany(t => t.EnumerateObject()).ToList();
        var entries = libraries
            .GroupBy(l => Id(l.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var direct = root.GetProperty("projectFileDependencyGroups").EnumerateObject()
            .SelectMany(g => g.Value.EnumerateArray())
            .Select(d => d.GetString()!.Split(' ', 2)[0]);

        var cameFrom = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { [projectName] = null };
        var queue = new Queue<string>();
        foreach (var id in direct.Where(id => cameFrom.TryAdd(id, projectName)))
            queue.Enqueue(id);
        while (queue.TryDequeue(out var current))
        {
            if (!entries.TryGetValue(current, out var entry) || !entry.Value.TryGetProperty("dependencies", out var deps))
                continue;
            foreach (var next in deps.EnumerateObject().Select(d => d.Name).Where(n => cameFrom.TryAdd(n, current)))
                queue.Enqueue(next);
        }

        return entries.Values
            .Where(e => e.Value.GetProperty("type").GetString() == "package")
            .Select(e => new ResolvedPackage(Id(e.Name), e.Name[(e.Name.IndexOf('/') + 1)..], PathTo(Id(e.Name), cameFrom)))
            .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// The files packages contribute to the build output, as paths relative to it: managed and native libraries
    /// at the top, platform-specific ones under runtimes/. If one is missing, the output isn't complete enough to
    /// inspect. A project built for one platform (a runtime identifier, such as the Windows application) ships only
    /// that platform's files, with native ones at the top, so only its platform target counts.
    /// </summary>
    public static IReadOnlyList<string> OutputFiles(string assetsJson)
    {
        using var doc = JsonDocument.Parse(assetsJson);
        var allTargets = doc.RootElement.GetProperty("targets").EnumerateObject().ToList();
        var platformTargets = allTargets.Where(t => t.Name.Contains('/', StringComparison.Ordinal)).ToList();
        var files = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var library in (platformTargets.Count > 0 ? platformTargets : allTargets).SelectMany(t => t.Value.EnumerateObject()))
        {
            if (library.Value.GetProperty("type").GetString() != "package")
                continue;
            foreach (var kind in new[] { "runtime", "native" })
            {
                if (library.Value.TryGetProperty(kind, out var assets))
                    files.UnionWith(assets.EnumerateObject().Select(a => a.Name).Where(IsFile).Select(a => a[(a.LastIndexOf('/') + 1)..]));
            }
            if (library.Value.TryGetProperty("runtimeTargets", out var targets))
                files.UnionWith(targets.EnumerateObject().Select(a => a.Name).Where(IsFile));
        }
        return files.ToList();
    }

    private static IEnumerable<JsonElement> Targets(JsonElement root) =>
        root.GetProperty("targets").EnumerateObject().Select(t => t.Value);

    /// <summary>"_._" marks an empty folder in a package: nothing is copied.</summary>
    private static bool IsFile(string asset) => !asset.EndsWith("/_._", StringComparison.Ordinal);

    private static string Id(string nameAndVersion) => nameAndVersion.Split('/', 2)[0];

    private static IReadOnlyList<string> PathTo(string id, Dictionary<string, string?> cameFrom)
    {
        if (!cameFrom.ContainsKey(id))
            return [id];
        var path = new List<string>();
        for (string? at = id; at is not null; at = cameFrom[at])
            path.Insert(0, at);
        return path;
    }
}

/// <summary>The package ID patterns of privacy/denied-packages.txt.</summary>
public sealed class DeniedPackages
{
    private readonly IReadOnlyList<(string Pattern, Regex Regex)> _patterns;

    private DeniedPackages(IEnumerable<string> patterns) =>
        _patterns = patterns.Select(p => (p, new Regex("^" + Regex.Escape(p).Replace(@"\*", ".*") + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))).ToList();

    public static readonly Lazy<DeniedPackages> Repository =
        new(() => Parse(File.ReadAllText(Path.Combine(Repo.PrivacyDir, "denied-packages.txt"))));

    public int Count => _patterns.Count;

    public static DeniedPackages Parse(string text) => new(text.Split('\n')
        .Select(line => line.Trim())
        .Where(line => line.Length > 0 && !line.StartsWith('#')));

    /// <summary>The pattern that denies a package, or null if it isn't denied.</summary>
    public string? Denies(string packageId) =>
        _patterns.FirstOrDefault(p => p.Regex.IsMatch(packageId)).Pattern;
}
