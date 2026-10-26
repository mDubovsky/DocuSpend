using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using static DocuSpend.Categorization.Tests.TestTransactions;

namespace DocuSpend.Categorization.Tests;

/// <summary>Changing and removing user rules, and the bound on their priority (work/024-edit-rules/spec.md).</summary>
public class ChangeRuleTests
{
    private static readonly CategorizationConfig Start = CategorizationConfig.Default;
    private static readonly SubcategoryId Groceries = new("food.groceries");
    private static readonly SubcategoryId Rent = new("housing.rent");

    private static Condition Acme => new TextCondition("acme", TextMode.Contains);

    private static CategorizationConfig Ok(Outcome<CategorizationConfig> outcome)
    {
        Assert.True(outcome.IsSuccess, outcome.Refusal?.Message);
        return outcome.Value!;
    }

    private static RefusalKind Refused(Outcome<CategorizationConfig> outcome)
    {
        Assert.False(outcome.IsSuccess);
        return outcome.Refusal!.Kind;
    }

    private static string? Decide(CategorizationConfig config, Statements.Model.Transaction t) =>
        config.RulesInOrder().FirstOrDefault(r => r.Condition.Matches(t))?.Target.Value;

    // The bound

    [Fact]
    public void The_highest_user_priority_number_is_255() => Assert.Equal(255, CategorizationConfig.MaxUserPriority);

    [Theory]
    [InlineData(1)]
    [InlineData(255)]
    public void Priority_numbers_from_1_to_255_are_accepted(int priority)
    {
        var added = Ok(Start.AddUserRule(Acme, Groceries, priority));
        Ok(added.SetUserRulePriority(added.UserRules[0].Id, priority));
        Ok(added.ChangeUserRule(added.UserRules[0].Id, Acme, Groceries, priority));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(256)]
    [InlineData(int.MaxValue)]
    public void Priority_numbers_outside_1_to_255_are_refused(int priority)
    {
        var added = Ok(Start.AddUserRule(Acme, Groceries, 1));

        Assert.Equal(RefusalKind.InvalidPriority, Refused(Start.AddUserRule(Acme, Groceries, priority)));
        Assert.Equal(RefusalKind.InvalidPriority, Refused(added.SetUserRulePriority(added.UserRules[0].Id, priority)));
        Assert.Equal(RefusalKind.InvalidPriority, Refused(added.ChangeUserRule(added.UserRules[0].Id, Acme, Groceries, priority)));
    }

    [Fact]
    public void A_rule_on_top_of_a_loaded_rule_numbered_above_255_gets_255_and_wins()
    {
        var loaded = CategorizationConfig.Create(Start.Catalog, Start.DefaultRules,
            [new Rule(new RuleId("user-old"), RuleSource.User, 300, 1, Acme, Rent)]);

        var onTop = Ok(loaded.AddUserRuleOnTop(Acme, Groceries));

        Assert.Equal(255, onTop.UserRules[^1].Priority);
        Assert.Equal(Groceries.Value, Decide(onTop, Card("ACME Bratislava")));
    }

    [Fact]
    public void A_loaded_rule_numbered_above_255_still_wins_over_every_default_rule()
    {
        var loaded = CategorizationConfig.Create(Start.Catalog, Start.DefaultRules,
            [new Rule(new RuleId("user-old"), RuleSource.User, 300, 1, Acme, Rent)]);

        Assert.Equal(Rent.Value, Decide(loaded, Card("ACME Bratislava")));
    }

    // Changing a rule

    [Fact]
    public void A_changed_rule_keeps_its_identifier_and_creation_order_and_gets_the_new_values()
    {
        var two = Ok(Ok(Start.AddUserRule(Acme, Groceries, 2)).AddUserRule(new TextCondition("lidl", TextMode.Contains), Groceries, 2));
        var first = two.UserRules[0];
        var lidlRent = new TextCondition("lidl", TextMode.EqualTo);

        var changed = Ok(two.ChangeUserRule(first.Id, lidlRent, Rent, 5));

        var rule = changed.UserRules.Single(r => r.Id == first.Id);
        Assert.Equal(first.Order, rule.Order);
        Assert.Equal(RuleSource.User, rule.Source);
        Assert.Equal(lidlRent, rule.Condition);
        Assert.Equal(Rent, rule.Target);
        Assert.Equal(5, rule.Priority);
        Assert.Equal(two.UserRules.Select(r => r.Id), changed.UserRules.Select(r => r.Id));
    }

    [Fact]
    public void Of_two_rules_with_the_same_number_the_newer_still_wins_after_the_older_is_changed()
    {
        var older = Ok(Start.AddUserRule(Acme, Groceries, 1));
        var both = Ok(older.AddUserRule(Acme, Rent, 1));

        var changed = Ok(both.ChangeUserRule(both.UserRules[0].Id, new TextCondition("ACME", TextMode.Contains), Groceries, 1));

        Assert.Equal(Rent.Value, Decide(changed, Card("ACME Bratislava")));
    }

    [Fact]
    public void Changing_a_rule_changes_how_transactions_are_decided()
    {
        var added = Ok(Start.AddUserRule(Acme, Groceries, 1));

        var changed = Ok(added.ChangeUserRule(added.UserRules[0].Id, new AmountCondition(100m, null), Rent, 1));

        Assert.NotEqual(Groceries.Value, Decide(changed, Card("ACME Bratislava", -5m)));
        Assert.Equal(Rent.Value, Decide(changed, Transfer(-450m, name: "Prenajímateľ")));
    }

    [Fact]
    public void Changing_a_rule_refuses_what_adding_refuses_and_changes_nothing()
    {
        var added = Ok(Start.AddUserRule(Acme, Groceries, 1));
        var id = added.UserRules[0].Id;

        Assert.Equal(RefusalKind.EmptyText, Refused(added.ChangeUserRule(id, new TextCondition("  ", TextMode.Contains), Groceries, 1)));
        Assert.Equal(RefusalKind.InvalidAmountRange, Refused(added.ChangeUserRule(id, new AmountCondition(10m, 5m), Groceries, 1)));
        Assert.Equal(RefusalKind.UnknownTarget, Refused(added.ChangeUserRule(id, Acme, new SubcategoryId("nowhere"), 1)));
        Assert.Equal(Acme, added.UserRules.Single().Condition);
    }

    [Fact]
    public void Changing_an_unknown_or_default_rule_is_refused()
    {
        Assert.Equal(RefusalKind.UnknownRule, Refused(Start.ChangeUserRule(new RuleId("user-none"), Acme, Groceries, 1)));
        Assert.Equal(RefusalKind.UnknownRule, Refused(Start.ChangeUserRule(Start.DefaultRules[0].Id, Acme, Groceries, 1)));
    }

    // Removing a rule

    [Fact]
    public void A_removed_rule_no_longer_decides_and_the_others_stay()
    {
        var two = Ok(Ok(Start.AddUserRule(Acme, Groceries, 1)).AddUserRule(new TextCondition("lidl", TextMode.Contains), Rent, 1));

        var removed = Ok(two.RemoveUserRule(two.UserRules[0].Id));

        Assert.Equal([two.UserRules[1]], removed.UserRules);
        Assert.NotEqual(Groceries.Value, Decide(removed, Card("ACME Bratislava")));
        Assert.Equal(2, two.UserRules.Count);
    }

    [Fact]
    public void Removing_an_unknown_or_default_rule_is_refused()
    {
        Assert.Equal(RefusalKind.UnknownRule, Refused(Start.RemoveUserRule(new RuleId("user-none"))));
        Assert.Equal(RefusalKind.UnknownRule, Refused(Start.RemoveUserRule(Start.DefaultRules[0].Id)));
    }

    [Fact]
    public void After_the_last_rule_is_removed_a_rule_on_top_gets_number_1()
    {
        var added = Ok(Start.AddUserRule(Acme, Groceries, 7));

        var onTop = Ok(Ok(added.RemoveUserRule(added.UserRules[0].Id)).AddUserRuleOnTop(Acme, Groceries));

        Assert.Equal(1, onTop.UserRules.Single().Priority);
    }

    [Fact]
    public void No_change_to_user_rules_changes_the_default_rules()
    {
        var added = Ok(Start.AddUserRule(Acme, Groceries, 1));
        var changed = Ok(added.ChangeUserRule(added.UserRules[0].Id, Acme, Rent, 3));
        var removed = Ok(changed.RemoveUserRule(added.UserRules[0].Id));

        Assert.Equal(Start.DefaultRules, removed.DefaultRules);
        Assert.Equal(Start.DefaultRules, changed.DefaultRules);
    }

    [Fact]
    public void Changing_rules_categorizes_every_valid_sample_as_a_config_built_with_the_result()
    {
        var added = Ok(Ok(Start.AddUserRuleOnTop(new AmountCondition(null, 20m), Groceries)).AddUserRuleOnTop(Acme, Rent));
        var changed = Ok(added.ChangeUserRule(added.UserRules[0].Id, new TypeCondition([Statements.Model.TransactionType.CardPayment]), Rent, 4));
        var first = added.UserRules[0];
        var direct = CategorizationConfig.Create(Start.Catalog, Start.DefaultRules,
        [
            new Rule(first.Id, RuleSource.User, 4, first.Order, new TypeCondition([Statements.Model.TransactionType.CardPayment]), Rent),
            added.UserRules[1],
        ]);

        foreach (var statement in Samples.Valid.Select(Samples.Read))
        {
            Assert.Equal(
                Categorizer.Categorize(statement, direct).Transactions.Select(t => (t.Placement, t.Rule)),
                Categorizer.Categorize(statement, changed).Transactions.Select(t => (t.Placement, t.Rule)));
        }
    }
}
