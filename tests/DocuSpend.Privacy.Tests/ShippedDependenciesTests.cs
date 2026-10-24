using System.Reflection;
using System.Xml.Linq;

namespace DocuSpend.Privacy.Tests;

/// <summary>
/// privacy-dependencies on the real repository: every project that ships (everything under src/ and tools/) is
/// inspected in its build output, with every assembly it carries, against the reviewed lists in privacy/.
/// </summary>
[Trait("Check", "privacy-dependencies")]
public class ShippedDependenciesTests(ITestOutputHelper log)
{
    /// <summary>The configuration this test was built in; the shipping projects were built alongside it.</summary>
    private static readonly string Configuration = typeof(ShippedDependenciesTests).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "Configuration").Value!;

    private static readonly IReadOnlyList<AllowedDependency> Allowed =
        ReviewedLists.ParseAllowedDependencies(File.ReadAllText(Path.Combine(Repo.PrivacyDir, "allowed-dependencies.json")));

    private static readonly IReadOnlyList<ReviewedNativeLibrary> Native =
        ReviewedLists.ParseNativeLibraries(File.ReadAllText(Path.Combine(Repo.PrivacyDir, "native-libraries.json")));

    /// <summary>Every project file directly under a folder in src/ or tools/.</summary>
    public static IReadOnlyList<string> ShippingProjects() => Repo.Files()
        .Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        .Where(f => f.Split('/') is [("src" or "tools"), _, _])
        .Order(StringComparer.Ordinal)
        .ToList();

    public static TheoryData<string> Projects => new(ShippingProjects());

    [Fact]
    public void Every_project_that_ships_is_checked()
    {
        // Guards the discovery itself: if it found nothing, the theory below would pass for the wrong reason.
        var projects = ShippingProjects();

        Assert.Contains("src/DocuSpend.Categorization/DocuSpend.Categorization.csproj", projects);
        Assert.Contains("src/DocuSpend.Statements/DocuSpend.Statements.csproj", projects);
        Assert.Contains("tools/DocuSpend.SampleGenerator/DocuSpend.SampleGenerator.csproj", projects);
        Assert.Contains("tools/DocuSpend.StatementCheck/DocuSpend.StatementCheck.csproj", projects);
        Assert.DoesNotContain(projects, p => p.StartsWith("tests/", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void A_shipping_project_brings_no_unreviewed_networking_telemetry_or_native_code(string project)
    {
        var result = CheckProject(project);

        foreach (var line in result.Reported)
            log.WriteLine(line);
        Assert.True(result.Failures.Count == 0, string.Join(Environment.NewLine + Environment.NewLine, result.Failures));
    }

    [Fact]
    public void Every_review_is_still_needed()
    {
        // A review nothing uses any more would quietly allow the same networking back later.
        var results = ShippingProjects().Select(CheckProject).ToList();

        Assert.Empty(Allowed.Where(a => !results.Any(r => r.UsedAllowances.Contains(a))).Select(a => $"{a.Assembly} {a.Versions}"));
        Assert.Empty(Native.Where(n => !results.Any(r => r.UsedNativeEntries.Contains(n.File))).Select(n => n.File));
    }

    /// <summary>A shipping project's build output, for the configuration this test was built in.</summary>
    public static string OutputDirOf(string project)
    {
        var xml = XDocument.Load(Repo.PathOf(project));
        var framework = xml.Descendants("TargetFramework").Single().Value;
        return Path.Combine(Path.GetDirectoryName(Repo.PathOf(project))!, "bin", Configuration, framework);
    }

    private static DependencyCheckResult CheckProject(string project)
    {
        var xml = XDocument.Load(Repo.PathOf(project));
        var name = xml.Descendants("AssemblyName").SingleOrDefault()?.Value ?? Path.GetFileNameWithoutExtension(project);
        var projectDir = Path.GetDirectoryName(Repo.PathOf(project))!;
        var outputDir = OutputDirOf(project);
        var assets = File.ReadAllText(Path.Combine(projectDir, "obj", "project.assets.json"));
        var removed = xml.Descendants("PrivacyRemovedFromOutput").Select(e => (string)e.Attribute("Include")!).ToList();

        var ownPrograms = ShippingProjects().Select(p => Path.GetFileNameWithoutExtension(p)).ToList();
        var output = OutputInspection.Inspect(outputDir, name, NetworkingList.Repository.Value, ownPrograms);
        var present = Directory.EnumerateFiles(outputDir, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(outputDir, f).Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);
        var missing = DependencyCheck.MissingFromOutput(name, PackageAssets.OutputFiles(assets), present, removed);

        var result = DependencyCheck.Evaluate(output, Allowed, Native, PackageAssets.Packages(name, assets), DeniedPackages.Repository.Value);
        return result with { Failures = [.. missing, .. result.Failures] };
    }
}
