using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace DocuSpend.Privacy.Tests;

/// <summary>
/// privacy/BannedSymbols.txt is well formed, every entry names something that exists, overload families are
/// complete, and every project is built with it under its exact name (plan: the file-name guard).
/// </summary>
[Trait("Check", "privacy-no-networking-code")]
public class BanListTests
{
    /// <summary>
    /// The .NET runtime, plus the libraries whose networking the list also bans in our code (Avalonia's previewer
    /// protocol), taken from the application's build output, which this test project builds first.
    /// </summary>
    private static readonly Compilation Runtime = BanCompiler.Compile("", extraReferences:
        [Path.Combine(ShippedDependenciesTests.OutputDirOf("src/DocuSpend.Desktop/DocuSpend.Desktop.csproj"), "Avalonia.Remote.Protocol.dll")]);

    private static readonly IReadOnlyList<(string Id, string Message)> Entries = File.ReadLines(BanCompiler.BanFile)
        .Select(line => line.Trim())
        .Where(line => line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal))
        .Select(line => line.Split(';', 2))
        .Select(parts => (parts[0], parts.Length > 1 ? parts[1] : ""))
        .ToList();

    [Fact]
    public void The_list_has_entries_for_every_kind_of_networking()
    {
        var ids = Entries.Select(e => e.Id).ToHashSet();

        foreach (var expected in new[]
        {
            "N:System.Net.Http", "N:System.Net.Sockets", "N:System.Net.WebSockets", "N:System.Net.Mail",
            "T:System.Net.Dns", "N:System.Net.NetworkInformation", "N:System.Net.Security",
            "T:System.Diagnostics.Process", "T:System.Runtime.InteropServices.DllImportAttribute",
            "T:System.Runtime.InteropServices.LibraryImportAttribute", "T:System.Runtime.InteropServices.NativeLibrary",
            "M:System.Type.GetType(System.String)", "M:System.Reflection.Assembly.Load(System.String)",
        })
            Assert.Contains(expected, ids);
    }

    [Fact]
    public void Every_entry_explains_the_rule_the_decision_and_the_way_to_an_exception()
    {
        Assert.All(Entries, entry =>
        {
            Assert.StartsWith("privacy-no-networking-code: ", entry.Message);
            Assert.Contains("(decision 0006, docs/decisions/0006-privacy-approach-and-network-boundary.md)", entry.Message);
            Assert.EndsWith("To request an exception, see privacy/README.md.", entry.Message);
            Assert.DoesNotContain(";", entry.Message);
        });
    }

    [Fact]
    public void No_entry_is_listed_twice()
    {
        Assert.Empty(Entries.GroupBy(e => e.Id).Where(g => g.Count() > 1).Select(g => g.Key));
    }

    [Fact]
    public void Every_entry_names_a_symbol_that_exists()
    {
        // A misspelled entry would silently ban nothing.
        var unresolved = Entries
            .Where(e => DocumentationCommentId.GetSymbolsForDeclarationId(e.Id, Runtime).IsEmpty)
            .Select(e => e.Id);

        Assert.Empty(unresolved);
    }

    /// <summary>Methods that load a type, an assembly or a COM object by name. Each is listed overload by overload.</summary>
    public static TheoryData<string, string> Families => new()
    {
        { "System.Type", "GetType" },
        { "System.Type", "GetTypeFromProgID" },
        { "System.Type", "GetTypeFromCLSID" },
        { "System.Reflection.Assembly", "Load" },
        { "System.Reflection.Assembly", "LoadFrom" },
        { "System.Reflection.Assembly", "LoadFile" },
        { "System.Reflection.Assembly", "UnsafeLoadFrom" },
        { "System.Reflection.Assembly", "LoadWithPartialName" },
        { "System.Reflection.Assembly", "ReflectionOnlyLoad" },
        { "System.Reflection.Assembly", "ReflectionOnlyLoadFrom" },
        { "System.Reflection.Assembly", "GetType" },
        { "System.Reflection.Module", "GetType" },
        { "System.Activator", "CreateInstance" },
        { "System.Activator", "CreateInstanceFrom" },
        { "System.AppDomain", "Load" },
        { "System.AppDomain", "ExecuteAssembly" },
        { "System.AppDomain", "ExecuteAssemblyByName" },
        { "System.AppDomain", "CreateInstance" },
        { "System.AppDomain", "CreateInstanceAndUnwrap" },
        { "System.AppDomain", "CreateInstanceFrom" },
        { "System.AppDomain", "CreateInstanceFromAndUnwrap" },
        { "System.Runtime.InteropServices.Marshal", "BindToMoniker" },
    };

    [Theory]
    [MemberData(nameof(Families))]
    public void Every_overload_that_loads_by_name_is_banned(string type, string method)
    {
        // Overloads without parameters, or that take a Type the code already holds, load nothing by name.
        var overloads = Runtime.GetTypeByMetadataName(type)!.GetMembers(method).OfType<IMethodSymbol>()
            .Where(m => m.DeclaredAccessibility == Accessibility.Public)
            .Where(m => m.Parameters.Length > 0 && m.Parameters[0].Type.ToDisplayString() != "System.Type")
            .Select(m => m.GetDocumentationCommentId()!)
            .ToList();
        var banned = Entries.Select(e => e.Id).ToHashSet();
        var missing = overloads.Where(id => !banned.Contains(id)).ToList();

        Assert.NotEmpty(overloads);
        Assert.True(missing.Count == 0, "Not in privacy/BannedSymbols.txt: " + string.Join(", ", missing));
    }

    [Fact]
    public void Every_project_is_built_with_the_analyzer_and_the_list_under_its_exact_name()
    {
        var props = XDocument.Load(Repo.PathOf("Directory.Build.props"));
        var targets = XDocument.Load(Repo.PathOf("Directory.Build.targets"));

        var banFile = Single(props, "PrivacyBannedSymbols").Value;
        Assert.Equal(@"$(PrivacyDir)BannedSymbols.txt", banFile);
        Assert.Equal(@"$(MSBuildThisFileDirectory)privacy\", Single(props, "PrivacyDir").Value);
        Assert.True(File.Exists(BanCompiler.BanFile));

        // The file holds other package references and targets too (Avalonia's build telemetry), so find these by name.
        Assert.Single(targets.Descendants("PackageReference"),
            r => (string?)r.Attribute("Include") == "Microsoft.CodeAnalysis.BannedApiAnalyzers");
        Assert.Equal("$(PrivacyBannedSymbols)", (string?)Single(targets, "AdditionalFiles").Attribute("Include"));
        Assert.Contains(BanCompiler.RuleId, Single(targets, "WarningsAsErrors").Value);

        // The guard that fails the build when the ban is missing or switched off in a project.
        var guard = Assert.Single(targets.Descendants("Target"), t => (string?)t.Attribute("Name") == "PrivacyBanGuard");
        Assert.Equal("CoreCompile", (string?)guard.Attribute("BeforeTargets"));
        Assert.Equal(5, guard.Elements("Error").Count());
    }

    private static XElement Single(XDocument doc, string name) => Assert.Single(doc.Descendants(name));
}
