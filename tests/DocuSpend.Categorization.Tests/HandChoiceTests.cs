using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Results;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;
using static DocuSpend.Categorization.Tests.TestTransactions;

namespace DocuSpend.Categorization.Tests;

/// <summary>A sub-category chosen for one transaction alone (work/022-categorize-from-list/spec.md: "Len túto transakciu").</summary>
public class HandChoiceTests
{
    private static readonly SubcategoryId Taxi = new("transport.taxi");
    private static readonly SubcategoryId Games = new("leisure.games");
    private static readonly SubcategoryId Groceries = new("food.groceries");
    private static readonly CategorizationConfig Keyed = CategorizationConfig.Default.WithFingerprintKey(FingerprintKey.Create());

    private static string Where(CategorizedTransaction t) => t.Placement.Subcategory?.Value ?? t.Placement.Category.Value;

    private static CategorizationConfig Choose(CategorizationConfig config, Statement statement, int index, SubcategoryId target) =>
        config.SetHandChoice(Fingerprints.Of(statement, config.FingerprintKey!)[index], target).Value!;

    [Fact]
    public void A_hand_choice_moves_only_its_transaction_not_its_identical_twin()
    {
        var stall = Card("STANOK U JOZEFA", -2.40m);
        var statement = TestStatements.Of(stall, stall with { }, Card("LIDL"));

        var result = Categorizer.Categorize(statement, Choose(Keyed, statement, 1, Taxi));

        Assert.Equal(["uncategorized-out", "transport.taxi", "food.groceries"], result.Transactions.Select(Where));
        Assert.Equal([false, true, false], result.Transactions.Select(t => t.ByHand));
        Assert.Null(result.Transactions[1].Rule);
    }

    [Fact]
    public void A_hand_choice_wins_over_a_default_rule_a_user_rule_and_a_newer_rule()
    {
        var lidl = Card("LIDL BRATISLAVA");
        var statement = TestStatements.Of(lidl);
        var config = Keyed.AddUserRuleOnTop(new TextCondition("lidl", TextMode.Contains), Games).Value!;
        config = Choose(config, statement, 0, Taxi);
        config = config.AddUserRuleOnTop(new TextCondition("bratislava", TextMode.Contains), Groceries).Value!;

        var result = Categorizer.Categorize(statement, config);

        Assert.Equal("transport.taxi", Where(result.Transactions[0]));
        Assert.True(result.Transactions[0].ByHand);
    }

    [Fact]
    public void A_hand_choice_counts_in_the_totals_by_its_categorys_role()
    {
        var statement = TestStatements.Of(Card("LIDL", -40m), Card("BILLA", -10m));
        var config = Choose(Keyed, statement, 0, new SubcategoryId("own-transfers.other"));

        var result = Categorizer.Categorize(statement, config);

        Assert.Equal(10m, result.SpendingTotal);
        Assert.Equal(-40m, result.ExcludedTotal);
        Assert.False(result.Transactions[0].CountsAsSpending);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void A_second_hand_choice_for_the_same_transaction_replaces_the_first()
    {
        var statement = TestStatements.Of(Card("LIDL"));
        var config = Choose(Choose(Keyed, statement, 0, Taxi), statement, 0, Games);

        Assert.Single(config.HandChoices);
        Assert.Equal("leisure.games", Where(Categorizer.Categorize(statement, config).Transactions[0]));
    }

    [Fact]
    public void Removing_a_hand_choice_gives_the_transaction_back_to_the_rules()
    {
        var statement = TestStatements.Of(Card("LIDL"));
        var config = Choose(Keyed, statement, 0, Taxi);
        var print = Fingerprints.Of(statement, config.FingerprintKey!)[0];

        var removed = config.RemoveHandChoice(print);

        Assert.Empty(removed.HandChoices);
        var transaction = Categorizer.Categorize(statement, removed).Transactions[0];
        Assert.Equal("food.groceries", Where(transaction));
        Assert.False(transaction.ByHand);
        Assert.Same(removed, removed.RemoveHandChoice(print));
    }

    [Theory]
    [InlineData("uncategorized-out")]
    [InlineData("transport")]
    [InlineData("no.such")]
    public void A_hand_choice_needs_an_existing_sub_category(string target)
    {
        var statement = TestStatements.Of(Card("LIDL"));
        var print = Fingerprints.Of(statement, Keyed.FingerprintKey!)[0];

        var outcome = Keyed.SetHandChoice(print, new SubcategoryId(target));

        Assert.False(outcome.IsSuccess);
        Assert.Equal(RefusalKind.UnknownTarget, outcome.Refusal!.Kind);
    }

    [Fact]
    public void A_hand_choice_needs_a_key() =>
        Assert.Throws<InvalidOperationException>(() =>
            CategorizationConfig.Default.SetHandChoice(new Fingerprint(new string('a', 64)), Taxi));

    [Fact]
    public void A_key_once_set_stays()
    {
        var other = FingerprintKey.Create();

        Assert.Same(Keyed, Keyed.WithFingerprintKey(other));
        Assert.Same(Keyed.FingerprintKey, Keyed.WithFingerprintKey(other).FingerprintKey);
    }

    [Fact]
    public void Rule_changes_and_new_categories_keep_the_hand_choices_and_the_key()
    {
        var statement = TestStatements.Of(Card("LIDL"));
        var config = Choose(Keyed, statement, 0, Taxi);

        var changed = config
            .AddUserRuleOnTop(new TextCondition("x", TextMode.Contains), Games).Value!
            .AddCategory("Šport", CategoryRole.Spending).Value!;
        changed = changed.RemoveUserRule(changed.UserRules[0].Id).Value!;

        Assert.Equal(config.HandChoices, changed.HandChoices);
        Assert.Same(config.FingerprintKey, changed.FingerprintKey);
    }

    [Fact]
    public void A_hand_choice_applies_in_an_overlapping_statement_and_a_statement_downloaded_again()
    {
        var day = new DateOnly(2026, 3, 30);
        var lidl = Card("LIDL BRATISLAVA") with { BookingDate = day, ValueDate = day };
        var march = TestStatements.Of(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), Card("TESCO"), lidl);
        var overlapping = TestStatements.Of(new DateOnly(2026, 3, 15), new DateOnly(2026, 4, 15), Transfer(-5m, name: "Ján"), lidl with { });
        var again = march with { Transactions = march.Transactions.Select(t => t with { }).ToList() };
        var config = Choose(Keyed, march, 1, Taxi);

        Assert.Equal("transport.taxi", Where(Categorizer.Categorize(overlapping, config).Transactions[1]));
        Assert.Equal("transport.taxi", Where(Categorizer.Categorize(again, config).Transactions[1]));
    }

    [Fact]
    public void A_transaction_whose_text_changed_is_not_recognised()
    {
        var statement = TestStatements.Of(Card("LIDL BRATISLAVA"));
        var config = Choose(Keyed, statement, 0, Taxi);
        var changed = TestStatements.Of(Card("LIDL BRATISLAVA 2"));

        var transaction = Categorizer.Categorize(changed, config).Transactions[0];

        Assert.Equal("food.groceries", Where(transaction));
        Assert.False(transaction.ByHand);
    }

    [Fact]
    public void A_hand_choice_no_statement_matches_is_kept_and_does_nothing()
    {
        var config = Choose(Keyed, TestStatements.Of(Card("LIDL")), 0, Taxi);

        var result = Categorizer.Categorize(TestStatements.Of(Card("TESCO")), config);

        Assert.Single(config.HandChoices);
        Assert.Equal("food.groceries", Where(result.Transactions[0]));
    }

    [Fact]
    public void Every_transaction_has_its_fingerprint_once_there_is_a_key_and_none_before()
    {
        var statement = TestStatements.Of(Card("LIDL"), Transfer());

        Assert.All(Categorizer.Categorize(statement, CategorizationConfig.Default).Transactions, t => Assert.Null(t.Fingerprint));
        Assert.Equal(
            Fingerprints.Of(statement, Keyed.FingerprintKey!),
            Categorizer.Categorize(statement, Keyed).Transactions.Select(t => t.Fingerprint!.Value));
    }

    [Fact]
    public void Hand_choices_without_a_key_or_repeated_or_with_a_missing_target_break_the_configuration()
    {
        var print = new Fingerprint(new string('a', 64));
        var key = FingerprintKey.Create();
        var start = CategorizationConfig.Default;

        Assert.Throws<ArgumentException>(() => CategorizationConfig.Create(start.Catalog, start.DefaultRules, handChoices: [new(print, Taxi)]));
        Assert.Throws<ArgumentException>(() => CategorizationConfig.Create(start.Catalog, start.DefaultRules,
            handChoices: [new(print, Taxi), new(print, Games)], fingerprintKey: key));
        Assert.Throws<ArgumentException>(() => CategorizationConfig.Create(start.Catalog, start.DefaultRules,
            handChoices: [new(print, new SubcategoryId("no.such"))], fingerprintKey: key));
    }
}
