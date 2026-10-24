namespace DocuSpend.Privacy.Tests;

/// <summary>
/// Avalonia's build telemetry stays off: no project resolves Avalonia.BuildServices with its build assets, which would
/// import the task that sends usage data on every build (privacy/README.md).
/// </summary>
[Trait("Check", "privacy-dependencies")]
public class BuildTelemetryTests
{
    private static string Assets(string buildServicesEntry) => $$"""
        {
          "version": 3,
          "targets": {
            "net10.0": {
              "Avalonia/11.3.22": { "type": "package", "dependencies": { "Avalonia.BuildServices": "11.3.2" } },
              {{buildServicesEntry}}
            }
          },
          "projectFileDependencyGroups": { "net10.0": [ "Avalonia >= 11.3.22" ] }
        }
        """;

    private const string Included = """
        "Avalonia.BuildServices/11.3.2": { "type": "package", "build": { "buildTransitive/Avalonia.BuildServices.targets": {} } }
        """;

    private const string Excluded = """
        "Avalonia.BuildServices/11.3.2": { "type": "package", "build": { "buildTransitive/_._": {} } }
        """;

    private const string Unrelated = """
        "Other/1.0.0": { "type": "package", "build": { "build/Other.targets": {} } }
        """;

    [Fact]
    public void The_package_with_its_build_assets_fails()
    {
        var failure = Assert.Single(BuildTelemetry.Failures("App", Assets(Included)));

        Assert.Contains("App", failure);
        Assert.Contains("Avalonia.BuildServices", failure);
        Assert.Contains("buildTransitive/Avalonia.BuildServices.targets", failure);
        Assert.Contains("privacy/README.md", failure);
    }

    [Fact]
    public void The_package_with_its_assets_excluded_passes()
    {
        Assert.Empty(BuildTelemetry.Failures("App", Assets(Excluded)));
    }

    [Fact]
    public void Build_assets_of_other_packages_are_not_its_business()
    {
        Assert.Empty(BuildTelemetry.Failures("App", Assets(Unrelated)));
    }

    public static TheoryData<string> RestoredProjects => new(BuildTelemetry.RestoredProjects());

    [Theory]
    [MemberData(nameof(RestoredProjects))]
    public void No_project_in_the_repository_runs_the_build_telemetry(string project)
    {
        Assert.Empty(BuildTelemetry.Failures(project, File.ReadAllText(BuildTelemetry.AssetsFileOf(project))));
    }

    public static TheoryData<string> Workflows => new(Directory.EnumerateFiles(Repo.PathOf(".github/workflows"), "*.yml")
        .Select(Path.GetFileName).Order(StringComparer.Ordinal)!);

    [Theory]
    [MemberData(nameof(Workflows))]
    public void Every_workflow_also_sets_avalonias_own_opt_out(string workflow)
    {
        var text = File.ReadAllText(Repo.PathOf(".github/workflows/" + workflow)).ReplaceLineEndings("\n");

        Assert.Contains("\nenv:\n", text);
        Assert.Contains("\n  AVALONIA_TELEMETRY_OPTOUT: \"1\"\n", text);
    }

    [Fact]
    public void The_application_resolves_the_package_so_the_check_looks_at_something()
    {
        // Guards the check itself: if the application stopped resolving the package, the theory above would pass
        // without having looked at it.
        const string project = "src/DocuSpend.Desktop/DocuSpend.Desktop.csproj";

        Assert.Contains(project, BuildTelemetry.RestoredProjects());
        Assert.True(BuildTelemetry.Resolves(File.ReadAllText(BuildTelemetry.AssetsFileOf(project))));
    }
}
