using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;

namespace DocuSpend.Presentation.Tests;

/// <summary>
/// The rule editor: the list in the order tried, editing, deleting and the built-in rules
/// (work/024-edit-rules/spec.md). The window's part is in DocuSpend.Desktop.Tests.
/// </summary>
public sealed class RuleEditorTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    private string RulesFile => Path.Combine(_folder, "rules.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private static readonly SubcategoryId Games = new("leisure.games");
    private static readonly SubcategoryId Rent = new("housing.rent");

    /// <summary>Saves the rules as given, in creation order, and starts DocuSpend on them with a sample open.</summary>
    private async Task<MainViewModel> StartWith(params (Condition Condition, SubcategoryId Target, int Priority)[] rules)
    {
        var config = CategorizationConfig.Default;
        foreach (var (condition, target, priority) in rules)
            config = config.AddUserRule(condition, target, priority).Value!;
        if (rules.Length > 0)
            Assert.True(new RuleStore(RulesFile).Save(config));
        return await Restart();
    }

    private async Task<MainViewModel> Restart(string sample = "01-typical-month")
    {
        var main = new MainViewModel(new RuleStore(RulesFile));
        await main.OpenFileAsync(Samples.Path(sample));
        Assert.True(main.HasStatement);
        return main;
    }

    private static Condition Text(string text) => new TextCondition(text, TextMode.Contains);

    private static void Target(RuleFormViewModel form, SubcategoryId target)
    {
        form.SelectedCategory = form.Categories.Single(c => c.Subcategories.Any(s => s.Id == target));
        form.SelectedSubcategory = form.Subcategories.Single(s => s.Id == target);
    }

    // The list

    [Fact]
    public async Task The_rules_are_listed_in_the_order_they_are_tried_with_their_numbers()
    {
        var main = await StartWith((Text("prvé"), Games, 3), (Text("druhé"), Games, 1), (Text("tretie"), Games, 1));

        Assert.Equal(
            [("1", "Text obsahuje „tretie“"), ("1", "Text obsahuje „druhé“"), ("3", "Text obsahuje „prvé“")],
            main.Rules.Select(r => (r.Priority, r.Condition)));
        Assert.Equal(
            main.Statement.Config.RulesInOrder().Where(r => r.Source == RuleSource.User).Select(r => r.Id),
            main.Rules.Select(r => r.Id));
    }

    [Fact]
    public async Task A_rule_added_through_the_form_is_listed_first()
    {
        var main = await StartWith((Text("prvé"), Games, 3), (Text("druhé"), Games, 2));
        main.RuleForm.Text = "nové";
        Target(main.RuleForm, Rent);

        main.AddRuleCommand.Execute(null);

        Assert.Equal(("2", "Text obsahuje „nové“"), (main.Rules[0].Priority, main.Rules[0].Condition));
        Assert.Equal(3, main.Rules.Count);
    }

    [Fact]
    public void The_priority_help_says_what_the_numbers_mean() =>
        Assert.Equal(
            "Pravidlá sa uplatňujú od priority 1. Transakciu zaradí prvé pravidlo, ktorému zodpovedá. Vaše pravidlá môžu mať prioritu od 1 do 255; vstavané pravidlá majú prioritu 256, takže vaše pravidlá majú vždy prednosť. Ak majú dve pravidlá rovnakú prioritu, rozhoduje novšie.",
            Texts.PriorityHelp);
}
