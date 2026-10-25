using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;

namespace DocuSpend.Presentation.Tests;

/// <summary>
/// The window's state: adding rules that are saved and survive a restart, and the choice of look (spec: adding a
/// rule, rules survive a restart). Opening statements is in <see cref="OpeningTests"/>.
/// </summary>
public sealed class MainViewModelTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    private string RulesFile => Path.Combine(_folder, "spike-rules.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    /// <summary>A fresh start of the application, with the rules file of this test, and a sample opened.</summary>
    private async Task<MainViewModel> Start(string sample = "01-typical-month")
    {
        var main = new MainViewModel(new SpikeRuleStore(RulesFile));
        await main.OpenFileAsync(Samples.Path(sample));
        Assert.True(main.HasStatement);
        return main;
    }

    private static void Target(RuleFormViewModel form, SubcategoryId target)
    {
        form.SelectedCategory = form.Categories.Single(c => c.Subcategories.Any(s => s.Id == target));
        form.SelectedSubcategory = form.Subcategories.Single(s => s.Id == target);
    }

    private static readonly SubcategoryId Games = new("leisure.games");

    /// <summary>Adds a text rule for one of the shown statement's uncategorized payments, and returns its description.</summary>
    private static string AddRuleForAnUncategorizedPayment(MainViewModel main)
    {
        main.Statement.SelectedCategory = new CategoryId("uncategorized-out");
        var description = main.Statement.Transactions[0].Transaction.Description!;
        main.RuleForm.SelectedKind = ConditionKind.Text;
        main.RuleForm.Text = description;
        main.RuleForm.SelectedTextMode = main.RuleForm.TextModes.Single(m => m.Mode == TextMode.EqualTo);
        Target(main.RuleForm, Games);
        main.AddRuleCommand.Execute(null);
        return description;
    }

    [Fact]
    public async Task An_added_rule_categorizes_again_at_once_and_is_listed()
    {
        var main = await Start();

        var description = AddRuleForAnUncategorizedPayment(main);

        Assert.Null(main.RuleForm.Error);
        Assert.Contains(main.Statement.Categories, c => c.Category == new CategoryId("leisure"));
        var placed = main.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description);
        Assert.All(placed, t => Assert.Equal(Games, t.Placement.Subcategory));
        var rule = Assert.Single(main.Rules);
        Assert.Contains(description, rule.Condition);
        Assert.Equal("Zábava a oddych › Hry a hračky", rule.Target);
        Assert.Equal(Texts.RuleMatched(placed.Count()), main.RuleForm.Message);
    }

    [Fact]
    public async Task An_added_rule_beats_every_earlier_rule()
    {
        var main = await Start();
        var description = AddRuleForAnUncategorizedPayment(main);

        main.RuleForm.SelectedKind = ConditionKind.Text;
        main.RuleForm.Text = description;
        Target(main.RuleForm, new SubcategoryId("food.groceries"));
        main.AddRuleCommand.Execute(null);

        Assert.All(main.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(new SubcategoryId("food.groceries"), t.Placement.Subcategory));
    }

    [Fact]
    public async Task A_rule_survives_a_restart()
    {
        var description = AddRuleForAnUncategorizedPayment(await Start());

        var restarted = await Start();

        Assert.Single(restarted.Rules);
        Assert.All(restarted.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(Games, t.Placement.Subcategory));
    }

    [Fact]
    public async Task Saved_rules_apply_to_every_statement_opened()
    {
        var main = await Start();
        main.RuleForm.SelectedKind = ConditionKind.Type;
        main.RuleForm.SelectedType = main.RuleForm.Types[0];
        Target(main.RuleForm, Games);
        main.AddRuleCommand.Execute(null);

        await main.OpenFileAsync(Samples.Path("02-busy-month"));

        var expected = Categorizer.Categorize(Samples.Read("02-busy-month"), main.Statement.Config);
        Assert.Equal(expected.Transactions.Select(t => t.Placement), main.Statement.Categorized!.Transactions.Select(t => t.Placement));
        Assert.Contains(main.Statement.Categorized!.Transactions, t => t.Placement.Subcategory == Games);
    }

    [Fact]
    public async Task Rules_saved_before_are_applied_to_a_statement_opened_after_a_restart()
    {
        var description = AddRuleForAnUncategorizedPayment(await Start());

        var restarted = new MainViewModel(new SpikeRuleStore(RulesFile));
        Assert.Single(restarted.Rules);
        await restarted.OpenFileAsync(Samples.Path("01-typical-month"));

        Assert.All(restarted.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(Games, t.Placement.Subcategory));
    }

    [Fact]
    public async Task A_rule_that_matches_nothing_is_saved_and_says_so()
    {
        var main = await Start();
        main.RuleForm.SelectedKind = ConditionKind.Text;
        main.RuleForm.Text = "nič také tu nie je";
        Target(main.RuleForm, Games);

        main.AddRuleCommand.Execute(null);

        Assert.Single(main.Rules);
        Assert.Equal(Texts.RuleMatchedNothing, main.RuleForm.Message);
        Assert.Single((await Start()).Rules);
    }

    [Fact]
    public async Task Invalid_input_saves_nothing()
    {
        var main = await Start();
        main.RuleForm.SelectedKind = ConditionKind.Text;
        Target(main.RuleForm, Games);

        main.AddRuleCommand.Execute(null);

        Assert.Equal(Texts.Refusal(RefusalKind.EmptyText), main.RuleForm.Error);
        Assert.Empty(main.Rules);
        Assert.False(File.Exists(RulesFile));
    }

    [Fact]
    public async Task An_unreadable_rules_file_is_reported_and_never_overwritten()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(RulesFile, "{ damaged");

        var main = await Start();
        AddRuleForAnUncategorizedPayment(main);

        Assert.Equal(Texts.RulesFileUnreadable, main.RulesError);
        Assert.Equal(Texts.RulesCannotBeSaved, main.RuleForm.Error);
        Assert.Empty(main.Rules);
        Assert.Equal("{ damaged", File.ReadAllText(RulesFile));
    }

    [Fact]
    public void Rules_are_described_in_slovak()
    {
        Assert.Equal("Text obsahuje „netflix“", MainViewModel.Describe(new TextCondition("netflix", TextMode.Contains)));
        Assert.Equal("Text sa rovná „netflix“", MainViewModel.Describe(new TextCondition("netflix", TextMode.EqualTo)));
        Assert.Equal("Typ: Okamžitý prevod",
            MainViewModel.Describe(new TypeCondition([Statements.Model.TransactionType.InstantTransfer])));
        Assert.Equal("Suma od 10,00 € do 20,00 €", MainViewModel.Describe(new AmountCondition(10m, 20m)).Replace(' ', ' '));
        Assert.Equal("Suma od 10,00 €", MainViewModel.Describe(new AmountCondition(10m, null)).Replace(' ', ' '));
        Assert.Equal("Suma do 20,00 €", MainViewModel.Describe(new AmountCondition(null, 20m)).Replace(' ', ' '));
    }

    [Fact]
    public void The_look_offers_three_choices_in_slovak_and_follows_windows_at_first()
    {
        var main = new MainViewModel(new SpikeRuleStore(RulesFile));

        Assert.Equal([Look.LookChoice.System, Look.LookChoice.Light, Look.LookChoice.Dark], main.Looks.Select(l => l.Choice));
        Assert.Equal(["Podľa systému", "Svetlý", "Tmavý"], main.Looks.Select(l => l.Label));
        Assert.Equal(Look.LookChoice.System, main.Look);
    }

    [Fact]
    public void A_chosen_look_is_saved_and_in_effect_after_a_restart()
    {
        var settings = Path.Combine(_folder, "settings.json");
        var main = new MainViewModel(new SpikeRuleStore(RulesFile), new Look.SettingsStore(settings));

        main.SelectedLook = main.Looks.Single(l => l.Choice == Look.LookChoice.Dark);

        var restarted = new MainViewModel(new SpikeRuleStore(RulesFile), new Look.SettingsStore(settings));
        Assert.Equal(Look.LookChoice.Dark, restarted.Look);
        Assert.Equal(Look.LookChoice.Dark, restarted.SelectedLook.Choice);
    }

    [Fact]
    public async Task Changing_the_look_keeps_what_the_user_was_doing()
    {
        var main = await Start();
        main.Statement.SelectedCategory = new CategoryId("housing");
        main.RuleForm.Text = "rozpísané";

        main.SelectedLook = main.Looks.Single(l => l.Choice == Look.LookChoice.Light);

        Assert.Equal(new CategoryId("housing"), main.Statement.SelectedCategory);
        Assert.Equal("rozpísané", main.RuleForm.Text);
    }

    [Theory]
    [InlineData(1, "Pravidlo zaradilo 1 transakciu.")]
    [InlineData(3, "Pravidlo zaradilo 3 transakcie.")]
    [InlineData(5, "Pravidlo zaradilo 5 transakcií.")]
    public void The_count_of_matched_transactions_is_in_correct_slovak(int count, string expected)
    {
        Assert.Equal(expected, Texts.RuleMatched(count));
    }
}
