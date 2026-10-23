using System.Text.RegularExpressions;

namespace DocuSpend.Privacy.Tests;

/// <summary>
/// Finds ways of switching the networking ban off that the build itself can't see: suppressing the warning in
/// code, lowering its severity in an analyzer config, or overriding the build wiring in a project file.
/// The build guard in Directory.Build.targets covers the rest (a missing analyzer or list, disabled analyzers).
/// </summary>
public static partial class SuppressionScan
{
    private static readonly string RuleId = BanCompiler.RuleId;

    /// <summary>The two files that wire up the ban; they are checked by BanListTests instead.</summary>
    private static readonly HashSet<string> Wiring = ["Directory.Build.props", "Directory.Build.targets"];

    /// <summary>Project-file settings that would turn the ban off or route around it, outside the wiring files.</summary>
    private static readonly string[] ProjectFileMarkers =
    [
        "BannedApiAnalyzers", "BannedSymbols", "PrivacyBannedSymbols", "PrivacyDir", "PrivacyBanAnalyzerVersion",
        "RunAnalyzers", "ImportDirectoryBuildProps", "ImportDirectoryBuildTargets", "EditorConfigFiles",
        "GlobalAnalyzerConfigFiles", "CodeAnalysisRuleSet", "SkipAnalyzers",
    ];

    /// <summary>Whether a file is one the scan looks at: C# code, analyzer configs and project files.</summary>
    public static bool IsScanned(string path)
    {
        var name = path[(path.LastIndexOf('/') + 1)..];
        return name is ".editorconfig" or ".globalconfig"
            || Path.GetExtension(name).ToLowerInvariant() is ".cs" or ".globalconfig" or ".ruleset" or ".csproj" or ".props" or ".targets" or ".rsp";
    }

    /// <summary>Problems in one file, given its repository-relative path (forward slashes) and its text.</summary>
    public static IEnumerable<string> Find(string path, string text)
    {
        var name = path[(path.LastIndexOf('/') + 1)..];
        var extension = Path.GetExtension(name).ToLowerInvariant();

        if (name is "Directory.Build.props" or "Directory.Build.targets" or "Directory.Build.rsp" && !Wiring.Contains(path))
            yield return $"{path}: a nested {name} replaces or bypasses the root one, which wires up the networking ban.";

        if (extension == ".cs")
        {
            foreach (var (line, number) in Lines(text))
            {
                var pragma = PragmaDisable().Match(line);
                if (pragma.Success && pragma.Groups["ids"].Value.Trim().Length == 0)
                    yield return $"{path}:{number}: '#pragma warning disable' without IDs also silences the networking ban.";
                else if (line.Contains(RuleId, StringComparison.OrdinalIgnoreCase))
                    yield return $"{path}:{number}: the networking ban ({RuleId}) must not be suppressed in code.";
            }
        }
        else if (name is ".editorconfig" or ".globalconfig" || extension is ".globalconfig" or ".ruleset")
        {
            foreach (var (line, number) in Lines(text))
            {
                if (line.Contains(RuleId, StringComparison.OrdinalIgnoreCase))
                    yield return $"{path}:{number}: the severity of the networking ban ({RuleId}) must not be changed.";
                else if (CategorySeverity().IsMatch(line))
                    yield return $"{path}:{number}: a severity setting for all analyzers or a whole category can silence the networking ban.";
            }
        }
        else if (extension is ".csproj" or ".props" or ".targets" or ".rsp" && !Wiring.Contains(path))
        {
            foreach (var (line, number) in Lines(text))
            {
                if (line.Contains(RuleId, StringComparison.OrdinalIgnoreCase))
                    yield return $"{path}:{number}: the networking ban ({RuleId}) must not be silenced in a project file.";
                foreach (var marker in ProjectFileMarkers.Where(m => line.Contains(m, StringComparison.OrdinalIgnoreCase)))
                    yield return $"{path}:{number}: '{marker}' changes how the networking ban is wired; only Directory.Build.props and Directory.Build.targets may set it.";
                if (RemovedAnalyzerInput().IsMatch(line))
                    yield return $"{path}:{number}: removing analyzers or additional files can remove the networking ban.";
            }
        }
    }

    private static IEnumerable<(string Line, int Number)> Lines(string text) =>
        text.Split('\n').Select((line, i) => (line.TrimEnd('\r'), i + 1));

    [GeneratedRegex(@"^\s*#\s*pragma\s+warning\s+disable\b(?<ids>[^/]*)")]
    private static partial Regex PragmaDisable();

    [GeneratedRegex(@"^\s*dotnet_analyzer_diagnostic\.", RegexOptions.IgnoreCase)]
    private static partial Regex CategorySeverity();

    [GeneratedRegex(@"<\s*(Analyzer|AdditionalFiles)\b[^>]*\bRemove\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex RemovedAnalyzerInput();
}
