using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Results;
using DocuSpend.Categorization.Rules;
using DocuSpend.SampleGenerator.Scenarios;
using DocuSpend.Statements.Model;

namespace DocuSpend.Categorization.Tests;

/// <summary>
/// Random statements and random rule sets, from fixed seeds so every run is the same, checked against an independent
/// oracle and the invariants of the spec: exactly one placement per transaction, nothing lost, categories adding up
/// to their sub-categories, newest wins among equal priorities.
/// </summary>
public class RandomizedInvariantTests
{
    public static TheoryData<int> Seeds => new(Enumerable.Range(1, 300));

    private static readonly string[] Words =
        ["TESCO", "NETFLIX", "COOP", "LEKAREN", "MZDA", "NAJOM", "SHELL", "OBI", "SPORIACI UCET", "POISTENIE", "MASIARSTVO", "KNIHY", "XTB", "BOLT", "AAA", "BBB"];

    private static readonly string[] Names = ["Ján Príkladný", "Úrad práce", "UNIQA", "Fiktívna Firma", "Eva Vzorová"];
    private static readonly string[] Accounts = ["SK31 0200 0000 0012 3456 7890", "SK12 1100 0000 0099 8877 6655", "DE89 3704 0044 0532 0130 00"];
    private static readonly string[] Symbols = ["1234", "0138", "9999"];

    private static readonly TransactionType[] Types = Enum.GetValues<TransactionType>();

    private static string? MaybeWord(SeededRandom r) => r.Next(4) == 0 ? null : $"{r.Pick(Words)} {r.Pick(Words)}".Trim();

    private static Transaction RandomTransaction(SeededRandom r)
    {
        var type = r.Pick(Types);
        var amount = r.Next(8) == 0 ? 0m : r.Amount(-400m, 400m);
        var day = new DateOnly(2026, 3, 1 + r.Next(28));
        return new Transaction
        {
            BookingDate = day,
            ValueDate = day,
            Type = type,
            Amount = amount,
            Currency = "EUR",
            Description = MaybeWord(r),
            PaymentPurpose = r.Next(3) == 0 ? MaybeWord(r) : null,
            CounterpartyName = r.Next(3) == 0 ? r.Pick(Names) : null,
            CounterpartyAccount = r.Next(3) == 0 ? r.Pick(Accounts) : null,
            VariableSymbol = r.Next(4) == 0 ? r.Pick(Symbols) : null,
            FeeAmount = type is TransactionType.AccountFee or TransactionType.FeeReversal ? amount : null,
        };
    }

    private static Condition RandomCondition(SeededRandom r)
    {
        var mode = r.Next(2) == 0 ? TextMode.Contains : TextMode.EqualTo;
        switch (r.Next(6))
        {
            case 0: return new TypeCondition(Enumerable.Range(0, 1 + r.Next(3)).Select(_ => r.Pick(Types)));
            case 1: return new TextCondition(r.Pick(Words), mode);
            case 2: return new CounterpartyNameCondition(r.Pick(Names), mode);
            case 3: return new CounterpartyAccountCondition(r.Pick(Accounts));
            case 4:
                var a = r.Amount(0m, 400m);
                var b = r.Amount(0m, 400m);
                return r.Next(3) switch
                {
                    0 => new AmountCondition(a, null),
                    1 => new AmountCondition(null, a),
                    _ => new AmountCondition(Math.Min(a, b), Math.Max(a, b)),
                };
            default: return new VariableSymbolCondition(r.Pick(Symbols));
        }
    }

    private static CategorizationConfig RandomConfig(SeededRandom r)
    {
        var config = CategorizationConfig.Default;
        if (r.Next(3) == 0)
            config = config.AddCategory("Moja kategória", (CategoryRole)r.Next(3)).Value!;

        var targets = config.Catalog.Categories.SelectMany(c => c.Subcategories).Select(s => s.Id).ToList();
        for (var i = r.Next(7); i > 0; i--)
        {
            var condition = RandomCondition(r);
            var target = r.Pick(targets);
            var outcome = r.Next(4) == 0
                ? config.AddUserRuleOnTop(condition, target)
                : config.AddUserRule(condition, target, 1 + r.Next(3));
            Assert.True(outcome.IsSuccess, outcome.Refusal?.Message);
            config = outcome.Value!;
        }
        return config;
    }

    private static Statement RandomStatement(SeededRandom r)
    {
        var transactions = Enumerable.Range(0, r.Next(41)).Select(_ => RandomTransaction(r)).ToList();
        var regular = transactions.Where(t => t.FeeAmount is null).ToList();
        var deposits = regular.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var withdrawals = regular.Where(t => t.Amount < 0).Sum(t => t.Amount);
        var fees = -transactions.Where(t => t.FeeAmount is not null).Sum(t => t.Amount);
        var opening = r.Amount(0m, 3000m);
        return new Statement
        {
            AccountHolder = "TEST",
            Iban = "SK00",
            Bic = "TESTSKXX",
            Currency = "EUR",
            PeriodStart = new DateOnly(2026, 3, 1),
            PeriodEnd = new DateOnly(2026, 3, 31),
            OpeningBalance = opening,
            ClosingBalance = opening + deposits + withdrawals - fees,
            TotalDeposits = deposits,
            TotalWithdrawals = withdrawals,
            TotalFees = fees,
            Transactions = transactions,
        };
    }

    /// <summary>The rule that should decide, found another way than the categorizer: the best match by key, not a sort.</summary>
    private static Rule? Oracle(CategorizationConfig config, Transaction transaction)
    {
        var user = config.UserRules.Where(rule => rule.Condition.Matches(transaction))
            .MinBy(rule => (rule.Priority, -rule.Order));
        return user ?? config.DefaultRules.Where(rule => rule.Condition.Matches(transaction)).MinBy(rule => rule.Priority);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void The_deciding_rule_is_the_one_the_spec_names(int seed)
    {
        var random = new SeededRandom((ulong)seed);
        var config = RandomConfig(random);
        var statement = RandomStatement(random);

        var result = Categorizer.Categorize(statement, config);

        for (var i = 0; i < statement.Transactions.Count; i++)
            Assert.Equal(Oracle(config, statement.Transactions[i])?.Id, result.Transactions[i].Rule?.Id);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Every_transaction_has_exactly_one_placement_and_nothing_is_lost(int seed)
    {
        var random = new SeededRandom((ulong)seed);
        var config = RandomConfig(random);
        var statement = RandomStatement(random);

        var result = Categorizer.Categorize(statement, config);

        Assert.Equal(statement.Transactions.Count, result.Transactions.Count);
        Assert.All(result.Transactions, t =>
        {
            var (category, subcategory) = config.Catalog.Describe(t.Placement);
            Assert.Equal(category.IsUncategorized, subcategory is null);
        });
        Assert.Equal(statement.Transactions.Count, result.Categories.Sum(c => c.Total.Count));
        Assert.Equal(statement.Transactions.Sum(t => t.Amount), result.Categories.Sum(c => c.Total.Amount));
        Assert.True(result.IsComplete);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void A_categorys_balance_is_the_sum_of_its_sub_categories(int seed)
    {
        var random = new SeededRandom((ulong)seed);
        var config = RandomConfig(random);

        var result = Categorizer.Categorize(RandomStatement(random), config);

        foreach (var category in result.Categories.Where(c => !c.Category.IsUncategorized))
        {
            Assert.Equal(category.Subcategories.Sum(s => s.Total.Amount), category.Total.Amount);
            Assert.Equal(category.Subcategories.Sum(s => s.Total.Count), category.Total.Count);
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void The_role_totals_follow_the_roles_of_the_categories_the_transactions_are_in(int seed)
    {
        var random = new SeededRandom((ulong)seed);
        var config = RandomConfig(random);

        var result = Categorizer.Categorize(RandomStatement(random), config);

        decimal InRole(CategoryRole role) => result.Transactions
            .Where(t => config.Catalog.FindCategory(t.Placement.Category)!.Role == role)
            .Sum(t => t.Transaction.Amount);
        Assert.Equal(-InRole(CategoryRole.Spending), result.SpendingTotal);
        Assert.Equal(InRole(CategoryRole.Income), result.IncomeTotal);
        Assert.Equal(InRole(CategoryRole.Excluded), result.ExcludedTotal);
        Assert.Equal(result.Transactions.Where(t => t.CountsAsSpending).Sum(t => -t.Transaction.Amount), result.SpendingTotal);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void The_same_input_gives_the_same_result(int seed)
    {
        var random = new SeededRandom((ulong)seed);
        var config = RandomConfig(random);
        var statement = RandomStatement(random);

        var first = Categorizer.Categorize(statement, config);
        var second = Categorizer.Categorize(statement, config);

        Assert.Equal(first.Transactions.Select(t => (t.Placement, t.Rule)), second.Transactions.Select(t => (t.Placement, t.Rule)));
        Assert.Equal((first.SpendingTotal, first.IncomeTotal, first.ExcludedTotal), (second.SpendingTotal, second.IncomeTotal, second.ExcludedTotal));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Among_equal_numbers_the_newest_matching_user_rule_decides(int seed)
    {
        var random = new SeededRandom((ulong)seed);
        var config = RandomConfig(random);
        var statement = RandomStatement(random);

        var result = Categorizer.Categorize(statement, config);

        foreach (var (transaction, categorized) in statement.Transactions.Zip(result.Transactions))
        {
            if (categorized.Rule is not { Source: RuleSource.User } decided)
                continue;
            var matching = config.UserRules.Where(r => r.Condition.Matches(transaction)).ToList();
            var best = matching.Min(r => r.Priority);
            var winner = matching.Where(r => r.Priority == best).MaxBy(r => r.Order)!;
            Assert.Equal(winner.Id, decided.Id);
        }
    }

    [Fact]
    public void The_random_cases_are_not_all_trivial()
    {
        // Guards the generators above: across the seeds there are ties, user rules deciding, and uncategorized money.
        var withTies = 0;
        var userDecided = 0;
        var bucketed = 0;
        foreach (var seed in Enumerable.Range(1, 300))
        {
            var random = new SeededRandom((ulong)seed);
            var config = RandomConfig(random);
            var result = Categorizer.Categorize(RandomStatement(random), config);
            withTies += config.UserRules.GroupBy(r => r.Priority).Any(g => g.Count() > 1) ? 1 : 0;
            userDecided += result.Transactions.Count(t => t.Rule?.Source == RuleSource.User);
            bucketed += result.Transactions.Count(t => t.Rule is null);
        }

        Assert.True(withTies > 30, $"{withTies}");
        Assert.True(userDecided > 100, $"{userDecided}");
        Assert.True(bucketed > 100, $"{bucketed}");
    }
}
