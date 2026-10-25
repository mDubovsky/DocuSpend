using System.Text.RegularExpressions;
using DocuSpend.Categorization.Model;
using DocuSpend.Presentation.Look;

namespace DocuSpend.Presentation.Tests;

/// <summary>
/// The statement is read in memory and nowhere else (spec: nothing is kept). These can't prove that nothing is ever
/// written, but they make a new place that writes something a deliberate change, and they check what is written.
/// </summary>
public sealed partial class NothingKeptTests : IDisposable
{
    private readonly string _appData = Directory.CreateTempSubdirectory("docuspend-appdata-").FullName;

    public void Dispose() => Directory.Delete(_appData, recursive: true);

    /// <summary>The only code allowed to write files: the look setting and the rules, neither of which holds a statement.</summary>
    private static readonly string[] Writers =
    [
        "src/DocuSpend.Presentation/Look/SettingsStore.cs",
        "src/DocuSpend.Presentation/SpikeRuleStore.cs",
    ];

    [GeneratedRegex(
        @"File\.(Write\w*|Create\w*|Copy|Move|Append\w*|Replace|Delete|SetAttributes|OpenWrite)\b" +
        @"|FileMode\.(Create|CreateNew|OpenOrCreate|Append|Truncate)\b|FileAccess\.(Write|ReadWrite)\b" +
        @"|\bStreamWriter\b|Directory\.(CreateDirectory|Move|Delete)\b|\.OpenWrite(Async)?\b")]
    private static partial Regex WritesFiles();

    private static string RepoRoot => Path.GetFullPath(Path.Combine(Samples.Dir, "..", ".."));

    [Fact]
    public void Only_the_settings_and_rules_stores_write_files()
    {
        var src = Path.Combine(RepoRoot, "src");
        var sep = Path.DirectorySeparatorChar;
        var writers = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{sep}bin{sep}") && !f.Contains($"{sep}obj{sep}"))
            .Where(f => WritesFiles().IsMatch(File.ReadAllText(f)))
            .Select(f => Path.GetRelativePath(RepoRoot, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(Writers, writers);
    }

    [Theory]
    [InlineData("File.WriteAllBytes(path, bytes);")]
    [InlineData("using var s = new FileStream(path, FileMode.Create);")]
    [InlineData("new FileStream(path, FileMode.Open, FileAccess.ReadWrite)")]
    [InlineData("using var w = new StreamWriter(path);")]
    [InlineData("File.Copy(a, b);")]
    [InlineData("Directory.CreateDirectory(cache);")]
    [InlineData("await file.OpenWriteAsync();")]
    public void The_check_catches_ways_of_writing_a_file(string code) => Assert.Matches(WritesFiles(), code);

    [Fact]
    public void The_check_leaves_reading_alone() =>
        Assert.DoesNotMatch(WritesFiles(), "new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)");

    [Fact]
    public async Task After_using_DocuSpend_nothing_it_wrote_mentions_a_statement()
    {
        var settingsFile = Path.Combine(_appData, "DocuSpend", "settings.json");
        var rulesFile = Path.Combine(_appData, "DocuSpend", "rules.json");
        var main = new MainViewModel(new SpikeRuleStore(rulesFile), new SettingsStore(settingsFile), Samples.Path("01-typical-month"));

        // Everything a user can do with statements: the example, every sample, a rule, the look.
        await main.OpenExampleCommand.ExecuteAsync(null);
        foreach (var name in Samples.Valid.Concat(Samples.Invalid))
            await main.OpenFileAsync(Samples.Path(name));
        main.SelectedLook = main.Looks.Single(l => l.Choice == LookChoice.Dark);
        main.RuleForm.SelectedKind = ConditionKind.Amount;
        main.RuleForm.AmountFrom = "1000";
        main.RuleForm.SelectedCategory = main.RuleForm.Categories.Single(c => c.Id == new CategoryId("housing"));
        main.RuleForm.SelectedSubcategory = main.RuleForm.Subcategories[0];
        main.AddRuleCommand.Execute(null);
        Assert.Single(main.Rules);

        var written = Directory.EnumerateFiles(_appData, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToList();
        Assert.Equal([rulesFile, settingsFile], written);
        var text = string.Concat(written.Select(File.ReadAllText));
        foreach (var forbidden in Forbidden())
            Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The samples' file names, their folder, and every identifying text inside the valid ones.</summary>
    private static IEnumerable<string> Forbidden()
    {
        yield return Samples.Dir;
        yield return Samples.Dir.Replace('\\', '/');
        yield return "samples";
        foreach (var name in Samples.Valid.Concat(Samples.Invalid))
            yield return name;
        foreach (var statement in Samples.Valid.Select(Samples.Read))
        {
            yield return statement.AccountHolder;
            yield return statement.Iban;
            foreach (var t in statement.Transactions)
            {
                foreach (var value in new[] { t.Description, t.CounterpartyName, t.CounterpartyAccount, t.Reference, t.PaymentPurpose })
                {
                    if (value is { Length: >= 6 })
                        yield return value;
                }
            }
        }
    }
}
