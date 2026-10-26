using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;
using static DocuSpend.Categorization.Tests.TestTransactions;

namespace DocuSpend.Categorization.Tests;

/// <summary>User rules and their order (spec: Rules, Priority, Deciding a transaction's category).</summary>
public class ConfigRuleTests
{
    private static readonly CategorizationConfig Start = CategorizationConfig.Default;
    private static readonly SubcategoryId Rent = new("housing.rent");
    private static readonly SubcategoryId Groceries = new("food.groceries");
    private static readonly SubcategoryId Salary = new("income.salary");

    private static Condition Coop => new TextCondition("coop", TextMode.Contains);

    private static CategorizationConfig Added(CategorizationConfig config, Condition condition, SubcategoryId target, int priority)
    {
        var outcome = config.AddUserRule(condition, target, priority);
        Assert.True(outcome.IsSuccess, outcome.Refusal?.Message);
        return outcome.Value!;
    }

    private static CategorizationConfig OnTop(CategorizationConfig config, Condition condition, SubcategoryId target)
    {
        var outcome = config.AddUserRuleOnTop(condition, target);
        Assert.True(outcome.IsSuccess, outcome.Refusal?.Message);
        return outcome.Value!;
    }

    /// <summary>The target of the first rule, in the order they are tried, that matches the transaction.</summary>
    private static string? Decide(CategorizationConfig config, Transaction t) =>
        config.RulesInOrder().FirstOrDefault(r => r.Condition.Matches(t))?.Target.Value;

    // Starting point

    [Fact]
    public void The_default_config_has_the_built_in_categories_default_rules_and_no_user_rules()
    {
        Assert.Equal(16, Start.Catalog.Categories.Count);
        Assert.NotEmpty(Start.DefaultRules);
        Assert.Empty(Start.UserRules);
    }

    // Adding a user rule

    [Fact]
    public void A_user_rule_is_added_with_a_generated_identifier_and_the_next_creation_order()
    {
        var one = Added(Start, Coop, Groceries, 1);
        var two = Added(one, new TextCondition("lidl", TextMode.Contains), Groceries, 2);

        Assert.Equal([1L, 2L], two.UserRules.Select(r => r.Order));
        Assert.All(two.UserRules, r =>
        {
            Assert.Equal(RuleSource.User, r.Source);
            Assert.StartsWith("user-", r.Id.Value);
        });
        Assert.NotEqual(two.UserRules[0].Id, two.UserRules[1].Id);
    }

    [Fact]
    public void Adding_a_rule_does_not_change_the_config_it_was_added_to()
    {
        Added(Start, Coop, Groceries, 1);

        Assert.Empty(Start.UserRules);
    }

    [Fact]
    public void A_user_rule_can_target_a_sub_category_of_a_user_category()
    {
        var withCategory = Start.AddCategory("Deti", CategoryRole.Spending).Value!;
        var target = withCategory.Catalog.Categories.Single(c => c.Name == "Deti").Subcategories.Single().Id;

        var outcome = withCategory.AddUserRule(new TextCondition("lego", TextMode.Contains), target, 1);

        Assert.True(outcome.IsSuccess, outcome.Refusal?.Message);
    }

    [Fact]
    public void Adding_categories_keeps_the_rules()
    {
        var withRule = Added(Start, Coop, Groceries, 1);

        Assert.Single(withRule.AddCategory("Deti", CategoryRole.Spending).Value!.UserRules);
        Assert.Single(withRule.AddSubcategory(new CategoryId("food"), "Káva").Value!.UserRules);
    }

    // Refusals

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_priority_below_one_is_refused(int priority) =>
        Assert.Equal(RefusalKind.InvalidPriority, Start.AddUserRule(Coop, Groceries, priority).Refusal!.Kind);

    [Fact]
    public void An_incomplete_condition_is_refused_with_its_own_reason() =>
        Assert.Equal(RefusalKind.EmptyText, Start.AddUserRule(new TextCondition(" ", TextMode.Contains), Groceries, 1).Refusal!.Kind);

    [Fact]
    public void An_amount_condition_without_bounds_is_refused() =>
        Assert.Equal(RefusalKind.InvalidAmountRange, Start.AddUserRule(new AmountCondition(null, null), Groceries, 1).Refusal!.Kind);

    [Theory]
    [InlineData("nope.nope")]
    [InlineData("food")]
    [InlineData("uncategorized-out")]
    public void A_target_that_is_not_an_existing_sub_category_is_refused(string target) =>
        Assert.Equal(RefusalKind.UnknownTarget, Start.AddUserRule(Coop, new SubcategoryId(target), 1).Refusal!.Kind);

    [Fact]
    public void A_refusal_has_a_message_without_user_content()
    {
        var refusal = Start.AddUserRule(new TextCondition("   ", TextMode.Contains), Groceries, 1).Refusal!;

        Assert.False(string.IsNullOrWhiteSpace(refusal.Message));
    }

    [Fact]
    public void A_refused_change_gives_no_config() =>
        Assert.Null(Start.AddUserRule(Coop, Groceries, 0).Value);

    // Equal priority numbers: the newest wins

    [Fact]
    public void Two_user_rules_may_share_a_priority_number()
    {
        var config = Added(Added(Start, Coop, Groceries, 5), new TextCondition("coop", TextMode.Contains), Rent, 5);

        Assert.Equal([5, 5], config.UserRules.Select(r => r.Priority));
    }

    [Fact]
    public void Of_two_matching_user_rules_with_the_same_number_the_newest_decides()
    {
        var config = Added(Added(Start, Coop, Groceries, 5), Coop, Rent, 5);

        Assert.Equal("housing.rent", Decide(config, Card("COOP JEDNOTA")));
    }

    [Fact]
    public void A_lower_number_wins_whatever_the_age()
    {
        var older = Added(Start, Coop, Groceries, 1);
        var config = Added(older, Coop, Rent, 2);

        Assert.Equal("food.groceries", Decide(config, Card("COOP JEDNOTA")));
    }

    [Fact]
    public void The_result_does_not_depend_on_the_order_the_rules_are_given_in_when_numbers_differ()
    {
        var a = Added(Added(Start, Coop, Groceries, 1), Coop, Rent, 2);
        var b = Added(Added(Start, Coop, Rent, 2), Coop, Groceries, 1);

        Assert.Equal(Decide(a, Card("COOP")), Decide(b, Card("COOP")));
    }

    // Changing a number later never changes the creation order

    [Fact]
    public void A_priority_can_be_changed_and_the_creation_order_stays()
    {
        var config = Added(Added(Start, Coop, Groceries, 5), Coop, Rent, 5);
        var first = config.UserRules[0];

        var changed = config.SetUserRulePriority(first.Id, 1).Value!;

        Assert.Equal(1, changed.UserRules.Single(r => r.Id == first.Id).Priority);
        Assert.Equal(first.Order, changed.UserRules.Single(r => r.Id == first.Id).Order);
        Assert.Equal("food.groceries", Decide(changed, Card("COOP")));
    }

    [Fact]
    public void Renumbering_a_rule_to_the_same_number_as_another_keeps_the_newest_winning()
    {
        var config = Added(Added(Start, Coop, Groceries, 1), Coop, Rent, 3);
        var older = config.UserRules[0];

        // Give the older rule the same number as the newer one: the newer one wins, as it was created later.
        var changed = config.SetUserRulePriority(older.Id, 3).Value!;

        Assert.Equal("housing.rent", Decide(changed, Card("COOP")));
    }

    [Fact]
    public void An_unknown_rule_cannot_be_renumbered() =>
        Assert.Equal(RefusalKind.UnknownRule, Start.SetUserRulePriority(new RuleId("nope"), 1).Refusal!.Kind);

    [Fact]
    public void A_rule_cannot_be_renumbered_below_one()
    {
        var config = Added(Start, Coop, Groceries, 5);

        Assert.Equal(RefusalKind.InvalidPriority, config.SetUserRulePriority(config.UserRules[0].Id, 0).Refusal!.Kind);
    }

    [Fact]
    public void A_default_rule_cannot_be_renumbered() =>
        Assert.Equal(RefusalKind.UnknownRule, Start.SetUserRulePriority(Start.DefaultRules[0].Id, 5).Refusal!.Kind);

    // Order of trying

    [Fact]
    public void User_rules_come_before_default_rules_whatever_the_numbers()
    {
        var config = Added(Start, Coop, Rent, 255);

        Assert.Equal(RuleSource.User, config.RulesInOrder().First().Source);
        Assert.Equal(
            config.DefaultRules.Select(r => r.Id),
            config.RulesInOrder().Skip(1).Select(r => r.Id));
    }

    [Fact]
    public void A_user_rule_beats_a_default_rule_that_matches_the_same_transaction()
    {
        // The default rules put COOP in groceries.
        Assert.Equal("food.groceries", Decide(Start, Card("COOP JEDNOTA")));

        var config = Added(Start, Coop, Rent, 255);

        Assert.Equal("housing.rent", Decide(config, Card("COOP JEDNOTA")));
    }

    [Fact]
    public void User_rules_are_tried_by_number_then_newest_first()
    {
        var config = Added(Added(Added(Start, Coop, Groceries, 2), Coop, Rent, 1), Coop, Salary, 2);

        Assert.Equal([1, 2, 2], config.RulesInOrder().Where(r => r.Source == RuleSource.User).Select(r => r.Priority));
        var userOrders = config.RulesInOrder().Where(r => r.Source == RuleSource.User).Select(r => r.Order);
        Assert.Equal([2L, 3L, 1L], userOrders);
    }

    // Adding on top

    [Fact]
    public void A_rule_added_on_top_with_no_user_rules_gets_number_one() =>
        Assert.Equal(1, OnTop(Start, Coop, Rent).UserRules.Single().Priority);

    [Fact]
    public void A_rule_added_on_top_takes_the_lowest_existing_user_number_and_wins()
    {
        var existing = Added(Added(Start, Coop, Groceries, 3), Coop, Salary, 5);

        var config = OnTop(existing, Coop, Rent);

        Assert.Equal(3, config.UserRules.Last().Priority);
        Assert.Equal("housing.rent", Decide(config, Card("COOP")));
    }

    [Fact]
    public void Several_rules_added_on_top_in_a_row_each_win_over_the_earlier_ones()
    {
        var config = Added(Start, Coop, Salary, 7);
        config = OnTop(config, Coop, Groceries);
        Assert.Equal("food.groceries", Decide(config, Card("COOP")));

        config = OnTop(config, Coop, Rent);
        Assert.Equal("housing.rent", Decide(config, Card("COOP")));
        Assert.All(config.UserRules.Skip(1), r => Assert.Equal(7, r.Priority));
    }

    [Fact]
    public void A_rule_on_top_beats_a_rule_with_the_number_one()
    {
        var config = OnTop(Added(Start, Coop, Salary, 1), Coop, Rent);

        Assert.Equal("housing.rent", Decide(config, Card("COOP")));
    }

    [Fact]
    public void A_rule_on_top_is_refused_for_the_same_reasons_as_any_rule()
    {
        Assert.Equal(RefusalKind.EmptyText, Start.AddUserRuleOnTop(new TextCondition("", TextMode.Contains), Rent).Refusal!.Kind);
        Assert.Equal(RefusalKind.UnknownTarget, Start.AddUserRuleOnTop(Coop, new SubcategoryId("nope")).Refusal!.Kind);
    }

    // Validating a config built from parts (as persistence will)

    [Fact]
    public void Default_rules_with_a_missing_target_are_rejected()
    {
        var bad = new Rule(new RuleId("d"), RuleSource.Default, 1, 1, Coop, new SubcategoryId("nope"));

        Assert.Throws<ArgumentException>(() => CategorizationConfig.Create(Start.Catalog, [bad]));
    }

    [Fact]
    public void Default_rules_with_the_same_number_are_rejected()
    {
        var a = new Rule(new RuleId("a"), RuleSource.Default, 1, 1, Coop, Rent);
        var b = new Rule(new RuleId("b"), RuleSource.Default, 1, 2, Coop, Groceries);

        Assert.Throws<ArgumentException>(() => CategorizationConfig.Create(Start.Catalog, [a, b]));
    }

    [Fact]
    public void Default_rules_below_number_one_are_rejected() =>
        Assert.Throws<ArgumentException>(() =>
            CategorizationConfig.Create(Start.Catalog, [new Rule(new RuleId("a"), RuleSource.Default, 0, 1, Coop, Rent)]));
}
