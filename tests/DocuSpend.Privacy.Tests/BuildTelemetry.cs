using System.Text.Json;

namespace DocuSpend.Privacy.Tests;

/// <summary>
/// Avalonia brings Avalonia.BuildServices, whose build task sends usage data to the Avalonia team on every build.
/// Directory.Build.targets references it with all its assets excluded, so the task is never imported. This reads a
/// project's obj/project.assets.json to prove it: an excluded package's build assets are only the "_._" placeholder.
/// </summary>
public static class BuildTelemetry
{
    public const string PackageId = "Avalonia.BuildServices";

    private static readonly string[] BuildKinds = ["build", "buildTransitive", "buildMultiTargeting"];

    /// <summary>One failure per build file of the package that would be imported.</summary>
    public static IReadOnlyList<string> Failures(string project, string assetsJson) =>
        BuildAssets(assetsJson)
            .Where(asset => !asset.EndsWith("/_._", StringComparison.Ordinal))
            .Select(asset =>
                $"{project} resolves {PackageId} with its build file {asset}, which would send Avalonia's build " +
                "telemetry on every build. It must stay excluded (Directory.Build.targets): see privacy/README.md.")
            .ToList();

    public static bool Resolves(string assetsJson)
    {
        using var doc = JsonDocument.Parse(assetsJson);
        return Entries(doc.RootElement).Any();
    }

    /// <summary>Every project in the repository that has been restored, tests included: the telemetry runs at build time.</summary>
    public static IReadOnlyList<string> RestoredProjects() => Repo.Files()
        .Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        .Where(f => File.Exists(AssetsFileOf(f)))
        .Order(StringComparer.Ordinal)
        .ToList();

    public static string AssetsFileOf(string project) =>
        Path.Combine(Path.GetDirectoryName(Repo.PathOf(project))!, "obj", "project.assets.json");

    /// <summary>The package's build files in every target, placeholders included.</summary>
    private static IReadOnlyList<string> BuildAssets(string assetsJson)
    {
        using var doc = JsonDocument.Parse(assetsJson);
        return Entries(doc.RootElement)
            .SelectMany(entry => BuildKinds.SelectMany(kind =>
                entry.TryGetProperty(kind, out var assets) ? assets.EnumerateObject().Select(a => a.Name) : []))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<JsonElement> Entries(JsonElement root) =>
        root.GetProperty("targets").EnumerateObject()
            .SelectMany(t => t.Value.EnumerateObject())
            .Where(l => l.Name.StartsWith(PackageId + "/", StringComparison.OrdinalIgnoreCase))
            .Select(l => l.Value);
}
