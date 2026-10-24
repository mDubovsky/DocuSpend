using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.BannedApiAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace DocuSpend.Privacy.Tests;

/// <summary>
/// Compiles a C# snippet in memory with the real banned-API analyzer and the real privacy/BannedSymbols.txt,
/// the same pair every project is built with, and returns what the analyzer reports.
/// </summary>
public static class BanCompiler
{
    public const string BanFileName = "BannedSymbols.txt";

    public static readonly string BanFile = Path.Combine(Repo.PrivacyDir, BanFileName);

    /// <summary>The ban diagnostic's ID. Assembled so that this source doesn't itself trip the suppression scan.</summary>
    public static readonly string RuleId = "RS" + "0030";

    /// <summary>The .NET runtime's own assemblies, the same ones a project compiles against.</summary>
    private static readonly ImmutableArray<MetadataReference> Runtime =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToImmutableArray();

    public static Compilation Compile(string source, OutputKind kind = OutputKind.DynamicallyLinkedLibrary,
        IEnumerable<string>? extraReferences = null)
    {
        var references = Runtime.AddRange((extraReferences ?? []).Select(p => MetadataReference.CreateFromFile(p)));
        return CSharpCompilation.Create(
            "Snippet",
            [CSharpSyntaxTree.ParseText(source, path: "Snippet.cs")],
            references,
            new CSharpCompilationOptions(kind, allowUnsafe: true));
    }

    /// <summary>The ban's findings on a snippet, analyzed with the ban list under the given file name.</summary>
    public static IReadOnlyList<Diagnostic> Violations(string source, OutputKind kind = OutputKind.DynamicallyLinkedLibrary,
        IEnumerable<string>? extraReferences = null, string banFileName = BanFileName)
    {
        var banList = new InMemoryText(Path.Combine(Repo.PrivacyDir, banFileName), File.ReadAllText(BanFile));
        var analyzed = Compile(source, kind, extraReferences)
            .WithAnalyzers([new CSharpSymbolIsBannedAnalyzer()], new AnalyzerOptions([banList]));
        return analyzed.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult()
            .Where(d => d.Id == RuleId)
            .ToList();
    }

    private sealed class InMemoryText(string path, string text) : AdditionalText
    {
        public override string Path => path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
    }
}
