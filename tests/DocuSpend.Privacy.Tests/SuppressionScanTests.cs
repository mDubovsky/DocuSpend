namespace DocuSpend.Privacy.Tests;

/// <summary>
/// The networking ban can't be switched off quietly: suppressions in code, analyzer configs and project files
/// are found (plan: ways around the ban are tested for). Fixtures are single-line strings with the rule ID
/// assembled at run time, so this file doesn't trip the scan it tests.
/// </summary>
[Trait("Check", "privacy-no-networking-code")]
public class SuppressionScanTests
{
    private static readonly string Rule = BanCompiler.RuleId;

    public static TheoryData<string, string> Bypasses => new()
    {
        { "src/A/Code.cs", $"#pragma warning disable {Rule}\nusing System;" },
        { "src/A/Code.cs", $"  #pragma warning disable CS0168, {Rule} // just this once" },
        { "src/A/Code.cs", "#pragma warning disable\nclass C { }" },
        { "src/A/Code.cs", "#pragma warning disable // everything" },
        { "src/A/GlobalSuppressions.cs", $"[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(\"ApiDesign\", \"{Rule}:Do not use banned APIs\")]" },
        { "src/A/Code.cs", $"[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"ApiDesign\", \"{Rule}\")] class C {{ }}" },
        { ".editorconfig", $"[*.cs]\ndotnet_diagnostic.{Rule}.severity = none" },
        { "src/A/.editorconfig", $"[*.cs]\ndotnet_diagnostic.{Rule.ToLowerInvariant()}.severity = suggestion" },
        { ".editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.category-ApiDesign.severity = none" },
        { ".globalconfig", "is_global = true\ndotnet_analyzer_diagnostic.severity = none" },
        { "src/A/rules.globalconfig", $"is_global = true\ndotnet_diagnostic.{Rule}.severity = none" },
        { "src/A/A.ruleset", $"<Rule Id=\"{Rule}\" Action=\"None\" />" },
        { "src/A/A.csproj", $"<NoWarn>$(NoWarn);{Rule}</NoWarn>" },
        { "src/A/A.csproj", $"<WarningsNotAsErrors>{Rule}</WarningsNotAsErrors>" },
        { "src/A/A.csproj", "<RunAnalyzers>false</RunAnalyzers>" },
        { "src/A/A.csproj", "<RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>" },
        { "src/A/A.csproj", "<ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>" },
        { "src/A/A.csproj", "<PrivacyBannedSymbols>empty.txt</PrivacyBannedSymbols>" },
        { "src/A/A.csproj", "<PackageReference Remove=\"Microsoft.CodeAnalysis.BannedApiAnalyzers\" />" },
        { "src/A/A.csproj", "<Analyzer Remove=\"@(Analyzer)\" />" },
        { "src/A/A.csproj", "<AdditionalFiles Remove=\"@(AdditionalFiles)\" />" },
        { "src/A/A.csproj", "<CodeAnalysisRuleSet>A.ruleset</CodeAnalysisRuleSet>" },
        { "src/A/A.csproj", "<GlobalAnalyzerConfigFiles Include=\"quiet.globalconfig\" />" },
        { "src/A/Directory.Build.props", "<Project />" },
        { "tools/Directory.Build.targets", "<Project />" },
        { "Directory.Build.rsp", "-p:PrivacyException=true" },
    };

    [Theory]
    [MemberData(nameof(Bypasses))]
    public void A_way_around_the_ban_is_found(string path, string text)
    {
        var problems = SuppressionScan.Find(path, text).ToList();

        Assert.NotEmpty(problems);
        Assert.All(problems, p => Assert.StartsWith(path, p));
        Assert.True(SuppressionScan.IsScanned(path), $"The repository scan would skip {path}");
    }

    public static TheoryData<string, string> Harmless => new()
    {
        { "src/A/Code.cs", "#pragma warning disable CS0168 // unused on purpose\nclass C { }" },
        { "src/A/Code.cs", "#pragma warning restore CS0168" },
        { "src/A/Code.cs", "[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Style\", \"IDE0060\")] class C { }" },
        { ".editorconfig", "[*.cs]\ndotnet_diagnostic.CA1822.severity = none\nindent_size = 4" },
        { "src/A/A.csproj", "<NoWarn>$(NoWarn);CS1591</NoWarn>" },
        { "src/A/A.csproj", "<PackageReference Include=\"PdfPig\" Version=\"0.1.16\" />" },
        { "src/A/A.csproj", "<PrivacyException>true</PrivacyException>" },
        { "Directory.Build.props", "<PrivacyBannedSymbols>$(PrivacyDir)BannedSymbols.txt</PrivacyBannedSymbols>" },
        { "Directory.Build.targets", $"<WarningsAsErrors>$(WarningsAsErrors);{Rule}</WarningsAsErrors>" },
        { "docs/notes.md", $"Never suppress {Rule}." },
    };

    [Theory]
    [MemberData(nameof(Harmless))]
    public void Ordinary_settings_are_not_flagged(string path, string text)
    {
        Assert.Empty(SuppressionScan.Find(path, text));
    }

    [Fact]
    public void A_problem_names_the_file_and_line()
    {
        var problem = Assert.Single(SuppressionScan.Find("src/A/Code.cs", $"class C {{ }}\n\n#pragma warning disable {Rule}"));

        Assert.StartsWith("src/A/Code.cs:3: ", problem);
    }

    [Fact]
    public void The_repository_has_no_way_around_the_ban()
    {
        var problems = Repo.Files()
            .Where(SuppressionScan.IsScanned)
            .SelectMany(path => SuppressionScan.Find(path, File.ReadAllText(Repo.PathOf(path))))
            .ToList();

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void The_repository_scan_sees_the_code()
    {
        // Guards the scan itself: if it stopped finding files, the test above would pass for the wrong reason.
        var files = Repo.Files().ToList();

        Assert.Contains("Directory.Build.targets", files);
        Assert.Contains("src/DocuSpend.Statements/Reading/VubStatementReader.cs", files);
        Assert.Contains("tests/DocuSpend.Privacy.Tests/SuppressionScanTests.cs", files);
        Assert.Contains("tools/DocuSpend.StatementCheck/DocuSpend.StatementCheck.csproj", files);
        Assert.DoesNotContain(files, f => f.StartsWith("samples/private/", StringComparison.Ordinal));
        Assert.DoesNotContain(files, f => f.Contains("/obj/", StringComparison.Ordinal) || f.Contains("/bin/", StringComparison.Ordinal));
    }
}
