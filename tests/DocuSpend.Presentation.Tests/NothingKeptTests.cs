using System.Globalization;
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
        "src/DocuSpend.Presentation/RuleStore.cs",
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
        // A damaged rules file from before, so the moved-aside copy is checked too.
        Directory.CreateDirectory(Path.GetDirectoryName(rulesFile)!);
        File.WriteAllText(rulesFile, "{ damaged");
        var main = new MainViewModel(new RuleStore(rulesFile), new SettingsStore(settingsFile), Samples.Path("01-typical-month"));
        var movedAside = Directory.EnumerateFiles(Path.GetDirectoryName(rulesFile)!).Single();

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
        // Every transaction of every valid sample categorized by hand, the note about fingerprints switched off on the way.
        var handChoices = 0;
        foreach (var name in Samples.Valid)
        {
            await main.OpenFileAsync(Samples.Path(name));
            handChoices += HandChooseEverything(main);
        }
        Assert.Equal(handChoices, main.Statement.Config.HandChoices.Count);

        var written = Directory.EnumerateFiles(_appData, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToList();
        Assert.Equal([movedAside, rulesFile, settingsFile], written);
        var text = string.Concat(written.Select(File.ReadAllText));
        foreach (var forbidden in Forbidden())
            Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Opening_folding_and_moving_between_rows_writes_nothing()
    {
        // work/045-transaction-details/spec.md: what is kept.
        var folder = Path.Combine(_appData, "DocuSpend");
        var main = new MainViewModel(new RuleStore(Path.Combine(folder, "rules.json")), new SettingsStore(Path.Combine(folder, "settings.json")));
        await main.OpenFileAsync(Samples.Path("02-busy-month"));
        var before = Snapshot();

        foreach (var category in main.Statement.Categories.Select(c => c.Category).ToList())
        {
            main.Statement.SelectedCategory = category;
            foreach (var row in main.Statement.Transactions)
            {
                main.Statement.SelectedTransaction = row;
                Assert.NotEmpty(row.Details);
            }
            main.EscapeCommand.Execute(null);
        }

        Assert.Equal(before, Snapshot());
    }

    /// <summary>Every file under the test's application data, with its content and when it was last written.</summary>
    private List<(string, string, DateTime)> Snapshot() =>
        Directory.EnumerateFiles(_appData, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(f => (f, File.ReadAllText(f), File.GetLastWriteTimeUtc(f)))
            .ToList();

    /// <summary>Puts each transaction of the open statement into the first sub-category, for it alone; returns how many.</summary>
    private static int HandChooseEverything(MainViewModel main)
    {
        var all = main.Statement.Categorized!.Transactions;
        for (var index = 0; index < all.Count; index++)
        {
            main.Statement.SelectedCategory = main.Statement.Categorized!.Transactions[index].Placement.Category;
            main.CategorizeCommand.Execute(main.Statement.Transactions.Single(r => r.Index == index));
            var form = main.Categorizing!.Form;
            form.SelectedCategory = form.Categories[0];
            form.SelectedSubcategory = form.Subcategories[0];
            main.ConfirmCategorizeCommand.Execute(null);
            if (main.Categorizing is { ShowsPrivacy: true } box)
            {
                box.HideNoteNextTime = true;
                main.ConfirmPrivacyCommand.Execute(null);
            }
            Assert.Null(main.Categorizing);
            Assert.True(main.Statement.Categorized!.Transactions[index].ByHand);
        }
        return all.Count;
    }

    private static string KeptSection()
    {
        var privacy = File.ReadAllText(Path.Join(RepoRoot, "PRIVACY.md"));
        var start = privacy.IndexOf("## What DocuSpend keeps", StringComparison.Ordinal);
        var end = privacy.IndexOf("## What it never does", StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, "PRIVACY.md has lost its section on what DocuSpend keeps.");
        return privacy[start..end];
    }

    /// <summary>
    /// The box about fingerprints and PRIVACY.md say the same about where a fingerprint is and how to remove it
    /// (work/022-categorize-from-list/spec.md). One is in Slovak and one in English, so the facts are compared.
    /// </summary>
    [Fact]
    public void The_box_about_fingerprints_and_PRIVACY_md_agree()
    {
        var box = string.Join(" ", Texts.FingerprintWhat, Texts.FingerprintLimits, Texts.FingerprintWhere);
        var kept = KeptSection();
        foreach (var fact in new[] { @"%APPDATA%\DocuSpend", "rules.json", "Zrušiť zaradenie" })
        {
            Assert.Contains(fact, box, StringComparison.Ordinal);
            Assert.Contains(fact, kept, StringComparison.Ordinal);
        }
        Assert.Contains("v tomto počítači", box, StringComparison.Ordinal);
        Assert.Contains("on this computer", kept, StringComparison.Ordinal);
        foreach (var detail in new[] { "dátum", "sumu", "popis", "protistranu" })
            Assert.Contains(detail, Texts.FingerprintWhat, StringComparison.Ordinal);
        foreach (var detail in new[] { "booking date", "amount", "description", "counterparty" })
            Assert.Contains(detail, kept, StringComparison.Ordinal);
        Assert.Contains(Texts.DontShowAgain, kept, StringComparison.Ordinal);
        Assert.Contains("by making a rule from a transaction", kept, StringComparison.Ordinal);
    }

    /// <summary>The samples' file names, their folder, and every identifying text, date and amount inside the valid ones.</summary>
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
                foreach (var date in new[] { t.BookingDate, t.ValueDate })
                {
                    yield return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    yield return SlovakFormat.Date(date);
                }
                // Amounts of at least 10, so that short numbers such as a priority can't match by chance.
                if (Math.Abs(t.Amount) >= 10)
                {
                    yield return Math.Abs(t.Amount).ToString("0.00", CultureInfo.InvariantCulture);
                    yield return Math.Abs(t.Amount).ToString("0.00", SlovakFormat.Culture);
                }
            }
        }
    }
}
