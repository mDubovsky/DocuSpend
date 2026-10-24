using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;

namespace DocuSpend.Presentation.Tests;

/// <summary>
/// The window's state: which sample is shown, and adding rules that are saved and survive a restart (spec: starting
/// and choosing a sample, adding a rule, rules survive a restart).
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

    /// <summary>A fresh start of the application, with the rules file of this test.</summary>
    private MainViewModel Start(IReadOnlyList<SampleItem>? samples = null) =>
        new(samples ?? SampleCatalog.Available(Samples.Dir), new SpikeRuleStore(RulesFile));

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
    public void The_first_sample_is_shown_at_start()
    {
        var main = Start();

        Assert.Equal("01-typical-month", main.SelectedSample?.Name);
        Assert.Equal(SlovakFormat.Period(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)), main.Statement.Period);
        Assert.Null(main.Error);
        Assert.Empty(main.Rules);
    }

    [Theory]
    [MemberData(nameof(StatementViewModelTests.Valid), MemberType = typeof(StatementViewModelTests))]
    public void Choosing_a_sample_shows_it(string name)
    {
        var main = Start();
        var statement = Samples.Read(name);

        main.SelectedSample = main.Samples.Single(s => s.Name == name);

        Assert.Equal(SlovakFormat.Period(statement.PeriodStart, statement.PeriodEnd), main.Statement.Period);
        Assert.Equal(SlovakFormat.Amount(statement.ClosingBalance), main.Statement.ClosingBalance);
    }

    [Fact]
    public void A_sample_that_cannot_be_read_shows_a_slovak_message()
    {
        Directory.CreateDirectory(_folder);
        var broken = Path.Combine(_folder, "broken.pdf");
        File.WriteAllBytes(broken, [1, 2, 3]);

        var main = Start([new SampleItem("broken", "Pokazený", broken)]);

        Assert.Equal(Texts.StatementUnreadable, main.Error);
    }

    [Fact]
    public void Without_samples_nothing_is_shown()
    {
        var main = Start([]);

        Assert.Null(main.SelectedSample);
        Assert.Empty(main.Statement.Categories);
    }

    [Fact]
    public void An_added_rule_categorizes_again_at_once_and_is_listed()
    {
        var main = Start();

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
    public void An_added_rule_beats_every_earlier_rule()
    {
        var main = Start();
        var description = AddRuleForAnUncategorizedPayment(main);

        main.RuleForm.SelectedKind = ConditionKind.Text;
        main.RuleForm.Text = description;
        Target(main.RuleForm, new SubcategoryId("food.groceries"));
        main.AddRuleCommand.Execute(null);

        Assert.All(main.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(new SubcategoryId("food.groceries"), t.Placement.Subcategory));
    }

    [Fact]
    public void A_rule_survives_a_restart()
    {
        var description = AddRuleForAnUncategorizedPayment(Start());

        var restarted = Start();

        Assert.Single(restarted.Rules);
        Assert.All(restarted.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(Games, t.Placement.Subcategory));
    }

    [Fact]
    public void Saved_rules_apply_to_every_sample()
    {
        var main = Start();
        main.RuleForm.SelectedKind = ConditionKind.Type;
        main.RuleForm.SelectedType = main.RuleForm.Types[0];
        Target(main.RuleForm, Games);
        main.AddRuleCommand.Execute(null);

        main.SelectedSample = main.Samples.Single(s => s.Name == "02-busy-month");

        var expected = Categorizer.Categorize(Samples.Read("02-busy-month"), main.Statement.Config);
        Assert.Equal(expected.Transactions.Select(t => t.Placement), main.Statement.Categorized!.Transactions.Select(t => t.Placement));
        Assert.Contains(main.Statement.Categorized!.Transactions, t => t.Placement.Subcategory == Games);
    }

    [Fact]
    public void A_rule_that_matches_nothing_is_saved_and_says_so()
    {
        var main = Start();
        main.RuleForm.SelectedKind = ConditionKind.Text;
        main.RuleForm.Text = "nič také tu nie je";
        Target(main.RuleForm, Games);

        main.AddRuleCommand.Execute(null);

        Assert.Single(main.Rules);
        Assert.Equal(Texts.RuleMatchedNothing, main.RuleForm.Message);
        Assert.Single(Start().Rules);
    }

    [Fact]
    public void Invalid_input_saves_nothing()
    {
        var main = Start();
        main.RuleForm.SelectedKind = ConditionKind.Text;
        Target(main.RuleForm, Games);

        main.AddRuleCommand.Execute(null);

        Assert.Equal(Texts.Refusal(RefusalKind.EmptyText), main.RuleForm.Error);
        Assert.Empty(main.Rules);
        Assert.False(File.Exists(RulesFile));
    }

    [Fact]
    public void An_unreadable_rules_file_is_reported_and_never_overwritten()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(RulesFile, "{ damaged");

        var main = Start();
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
        Assert.Equal("Suma od 10,00 € do 20,00 €", MainViewModel.Describe(new AmountCondition(10m, 20m)).Replace(' ', ' '));
        Assert.Equal("Suma od 10,00 €", MainViewModel.Describe(new AmountCondition(10m, null)).Replace(' ', ' '));
        Assert.Equal("Suma do 20,00 €", MainViewModel.Describe(new AmountCondition(null, 20m)).Replace(' ', ' '));
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
