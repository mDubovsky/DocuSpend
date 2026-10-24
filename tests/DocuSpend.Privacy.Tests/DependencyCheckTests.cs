namespace DocuSpend.Privacy.Tests;

/// <summary>
/// The dependency check fails on unreviewed networking, denied packages and unreviewed native files, and passes, while
/// still reporting, what a reviewer allowed (spec: privacy-dependencies).
/// </summary>
[Trait("Check", "privacy-dependencies")]
public class DependencyCheckTests
{
    private static readonly NetworkingFinding Net = new("Net.dll", "Net", new Version(2, 1, 0, 0),
        ["System.Net.Http.HttpClient"], ["App", "Toolkit", "Net"]);

    private static OutputReport Output(IReadOnlyList<NetworkingFinding>? findings = null, IReadOnlyList<string>? native = null) =>
        new("App", ["App.dll"], native ?? [], findings ?? []);

    private static readonly DeniedPackages Denied = DeniedPackages.Parse("Sentry*\n*ApplicationInsights*");

    private static DependencyCheckResult Check(OutputReport output, IReadOnlyList<AllowedDependency>? allowed = null,
        IReadOnlyList<ReviewedNativeLibrary>? native = null, IReadOnlyList<ResolvedPackage>? packages = null) =>
        DependencyCheck.Evaluate(output, allowed ?? [], native ?? [], packages ?? [], Denied);

    [Fact]
    public void A_clean_output_passes()
    {
        var result = Check(Output(), packages: [new ResolvedPackage("PdfPig", "0.1.16", ["App", "PdfPig"])]);

        Assert.Empty(result.Failures);
        Assert.Empty(result.Reported);
    }

    [Fact]
    public void Unreviewed_networking_fails_naming_the_assembly_the_use_the_path_and_what_to_do()
    {
        var failure = Assert.Single(Check(Output([Net])).Failures);

        Assert.StartsWith("privacy-dependencies: App ships Net 2.1.0.0 (Net.dll)", failure);
        Assert.Contains("System.Net.Http.HttpClient", failure);
        Assert.Contains("Path: App → Toolkit → Net.", failure);
        Assert.Contains("decision 0006", failure);
        Assert.Contains("privacy/allowed-dependencies.json", failure);
    }

    [Fact]
    public void Reviewed_networking_passes_and_is_still_reported()
    {
        var allowance = new AllowedDependency("Net", "[2.0, 3.0)", "Only used for images from web addresses, which we never load.", "A Reviewer");

        var result = Check(Output([Net]), [allowance]);

        Assert.Empty(result.Failures);
        var report = Assert.Single(result.Reported);
        Assert.Contains("allowed by review (A Reviewer)", report);
        Assert.Contains("Path: App → Toolkit → Net.", report);
        Assert.Contains(allowance, result.UsedAllowances);
    }

    [Theory]
    [InlineData("Net", "[3.0, 4.0)")]
    [InlineData("Net", "2.0.0")]
    [InlineData("Other", "[2.0, 3.0)")]
    [InlineData("net", "[2.0, 3.0)")]
    public void A_review_covers_only_its_assembly_and_versions(string assembly, string versions)
    {
        var result = Check(Output([Net]), [new AllowedDependency(assembly, versions, "Reviewed.", "A Reviewer")]);

        Assert.Single(result.Failures);
        Assert.Empty(result.UsedAllowances);
    }

    [Fact]
    public void A_denied_package_fails_wherever_it_is_in_the_tree()
    {
        var packages = new[]
        {
            new ResolvedPackage("Toolkit", "1.0.0", ["App", "Toolkit"]),
            new ResolvedPackage("Sentry.Extensions.Logging", "4.0.0", ["App", "Toolkit", "Logging", "Sentry.Extensions.Logging"]),
        };

        var failure = Assert.Single(Check(Output(), packages: packages).Failures);

        Assert.StartsWith("privacy-dependencies: App depends on Sentry.Extensions.Logging 4.0.0", failure);
        Assert.Contains("('Sentry*')", failure);
        Assert.Contains("Path: App → Toolkit → Logging → Sentry.Extensions.Logging.", failure);
    }

    [Theory]
    [InlineData("Microsoft.ApplicationInsights", true)]
    [InlineData("microsoft.applicationinsights.aspnetcore", true)]
    [InlineData("Serilog.Sinks.ApplicationInsights", true)]
    [InlineData("Sentry", true)]
    [InlineData("SentryLike", true)]
    [InlineData("PdfPig", false)]
    [InlineData("MySentry", false)]
    public void Denied_patterns_match_package_ids_ignoring_case(string id, bool denied)
    {
        Assert.Equal(denied, Denied.Denies(id) is not null);
    }

    [Fact]
    public void An_unreviewed_native_file_fails()
    {
        var failure = Assert.Single(Check(Output(native: ["runtimes/win-x64/native/foo.dll"])).Failures);

        Assert.Contains("native file runtimes/win-x64/native/foo.dll", failure);
        Assert.Contains("privacy/native-libraries.json", failure);
    }

    [Fact]
    public void A_reviewed_native_file_passes_and_is_still_reported()
    {
        var entry = new ReviewedNativeLibrary("runtimes/win-x64/native/foo.dll", "Image decoding; no networking imports.", "A Reviewer");

        var result = Check(Output(native: ["runtimes/win-x64/native/foo.dll"]), native: [entry]);

        Assert.Empty(result.Failures);
        Assert.Contains("native file runtimes/win-x64/native/foo.dll, reviewed (A Reviewer)", Assert.Single(result.Reported));
        Assert.Contains(entry.File, result.UsedNativeEntries);
    }

    [Fact]
    public void Package_files_missing_from_the_output_fail()
    {
        var failure = Assert.Single(DependencyCheck.MissingFromOutput("App", ["A.dll", "runtimes/win/lib/net10.0/B.dll"],
            ["A.dll", "App.dll"]));

        Assert.Contains("lacks runtimes/win/lib/net10.0/B.dll", failure);
    }

    [Fact]
    public void A_package_file_the_project_removes_from_its_output_is_not_missing()
    {
        Assert.Empty(DependencyCheck.MissingFromOutput("App", ["A.dll", "Designer.dll"], ["A.dll", "App.dll"], removed: ["Designer.dll"]));
    }

    [Fact]
    public void A_removed_file_that_is_still_in_the_output_fails()
    {
        var failure = Assert.Single(DependencyCheck.MissingFromOutput("App", ["A.dll", "Designer.dll"], ["A.dll", "Designer.dll"],
            removed: ["Designer.dll"]));

        Assert.Contains("Designer.dll", failure);
        Assert.Contains("PrivacyRemovedFromOutput", failure);
    }

    [Fact]
    public void Removing_a_file_no_package_provides_fails()
    {
        // A stale removal would read as a review that no longer means anything.
        var failure = Assert.Single(DependencyCheck.MissingFromOutput("App", ["A.dll"], ["A.dll"], removed: ["Designer.dll"]));

        Assert.Contains("Designer.dll", failure);
        Assert.Contains("no package provides", failure);
    }
}
