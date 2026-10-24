namespace DocuSpend.Privacy.Tests;

/// <summary>Packages, including indirect ones, and their output files are read from project.assets.json.</summary>
[Trait("Check", "privacy-dependencies")]
public class PackageAssetsTests
{
    /// <summary>App references Lib (a project), which brings Toolkit, which brings Sentry; App also uses PdfPig directly.</summary>
    private const string Assets = """
        {
          "version": 3,
          "targets": {
            "net10.0": {
              "PdfPig/0.1.16": { "type": "package", "runtime": { "lib/net9.0/UglyToad.PdfPig.dll": {} } },
              "Toolkit/2.0.0": {
                "type": "package",
                "dependencies": { "Sentry": "4.0.0" },
                "runtime": { "lib/net8.0/Toolkit.dll": {} },
                "native": { "runtimes/win-x64/native/toolkit_native.dll": {} },
                "runtimeTargets": { "runtimes/win/lib/net8.0/Toolkit.dll": { "assetType": "runtime", "rid": "win" } }
              },
              "Sentry/4.0.0": { "type": "package", "runtime": { "lib/net8.0/Sentry.dll": {} } },
              "Analyzers/1.0.0": { "type": "package", "build": { "buildTransitive/_._": {} }, "runtime": { "lib/netstandard2.0/_._": {} } },
              "Lib/1.0.0": { "type": "project", "dependencies": { "Toolkit": "2.0.0" }, "runtime": { "bin/placeholder/Lib.dll": {} } }
            }
          },
          "projectFileDependencyGroups": { "net10.0": [ "Lib >= 1.0.0", "PdfPig >= 0.1.16", "Analyzers >= 1.0.0" ] }
        }
        """;

    [Fact]
    public void Every_package_is_found_with_the_path_from_the_project()
    {
        var packages = PackageAssets.Packages("App", Assets).ToDictionary(p => p.Id);

        Assert.Equal(["Analyzers", "PdfPig", "Sentry", "Toolkit"], packages.Keys.Order());
        Assert.Equal("App → Lib → Toolkit → Sentry", packages["Sentry"].PathText);
        Assert.Equal("App → PdfPig", packages["PdfPig"].PathText);
        Assert.Equal("4.0.0", packages["Sentry"].Version);
    }

    [Fact]
    public void The_files_packages_put_in_the_output_are_listed()
    {
        Assert.Equal(
            ["Sentry.dll", "Toolkit.dll", "UglyToad.PdfPig.dll", "runtimes/win/lib/net8.0/Toolkit.dll", "toolkit_native.dll"],
            PackageAssets.OutputFiles(Assets));
    }

    /// <summary>
    /// A project built for one platform (a runtime identifier) has a second target for it, in which NuGet has already
    /// picked that platform's files; the platform-neutral target still lists every platform's.
    /// </summary>
    private const string PlatformAssets = """
        {
          "version": 3,
          "targets": {
            "net10.0": {
              "Skia/2.0.0": {
                "type": "package",
                "runtime": { "lib/net8.0/Skia.dll": {} },
                "runtimeTargets": {
                  "runtimes/win-x64/native/libskia.dll": { "assetType": "native", "rid": "win-x64" },
                  "runtimes/osx/native/libskia.dylib": { "assetType": "native", "rid": "osx" }
                }
              }
            },
            "net10.0/win-x64": {
              "Skia/2.0.0": {
                "type": "package",
                "runtime": { "lib/net8.0/Skia.dll": {} },
                "native": { "runtimes/win-x64/native/libskia.dll": {} }
              }
            }
          },
          "projectFileDependencyGroups": { "net10.0": [ "Skia >= 2.0.0" ] }
        }
        """;

    [Fact]
    public void A_project_built_for_one_platform_expects_only_that_platforms_files()
    {
        Assert.Equal(["Skia.dll", "libskia.dll"], PackageAssets.OutputFiles(PlatformAssets));
    }

    [Fact]
    public void A_denied_indirect_package_fails_the_check()
    {
        var packages = PackageAssets.Packages("App", Assets);
        var output = new OutputReport("App", ["App.dll"], [], []);

        var result = DependencyCheck.Evaluate(output, [], [], packages, DeniedPackages.Repository.Value);

        Assert.Contains("App → Lib → Toolkit → Sentry", Assert.Single(result.Failures));
    }
}
