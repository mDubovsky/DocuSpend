using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;
using static DocuSpend.Categorization.Tests.TestTransactions;

namespace DocuSpend.Categorization.Tests;

/// <summary>The six kinds of rule condition (spec: Conditions).</summary>
public class ConditionTests
{
    // Transaction type

    [Fact]
    public void A_type_condition_matches_any_of_its_types()
    {
        var condition = new TypeCondition([TransactionType.CardPayment, TransactionType.WalletCardPayment]);

        Assert.True(condition.Matches(Card("COOP")));
        Assert.True(condition.Matches(Card("COOP", wallet: true)));
        Assert.False(condition.Matches(Transfer()));
        Assert.False(condition.Matches(Transfer(type: TransactionType.InstantTransfer)));
    }

    [Fact]
    public void A_type_condition_can_match_the_fee_types() =>
        Assert.All([Fee(-7m), Fee(7m)], t => Assert.True(new TypeCondition([TransactionType.AccountFee, TransactionType.FeeReversal]).Matches(t)));

    [Fact]
    public void A_type_condition_needs_at_least_one_type() =>
        Assert.Equal(RefusalKind.NoTypeChosen, new TypeCondition([]).Validate()!.Kind);

    // Text in the description or the payment purpose

    [Theory]
    [InlineData(TextMode.Contains, "netflix", "NETFLIX.COM", null, true)]
    [InlineData(TextMode.Contains, "netflix", null, "Predplatne Netflix", true)]
    [InlineData(TextMode.Contains, "netflix", "SPOTIFY", "Hudba", false)]
    [InlineData(TextMode.EqualTo, "mzda", "MZDA", null, true)]
    [InlineData(TextMode.EqualTo, "mzda", null, "mzda", true)]
    [InlineData(TextMode.EqualTo, "mzda", "MZDA 02/2026", "Mzda za február", false)]
    [InlineData(TextMode.Contains, "lekaren", "LEKÁREŇ PRI PARKU", null, true)]
    public void A_text_condition_tests_the_description_and_the_purpose(TextMode mode, string text, string? description, string? purpose, bool expected) =>
        Assert.Equal(expected, new TextCondition(text, mode).Matches(Transfer(description: description, purpose: purpose)));

    [Fact]
    public void A_text_condition_never_matches_a_transaction_without_text() =>
        Assert.False(new TextCondition("x", TextMode.Contains).Matches(Transfer()));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_text_condition_needs_text(string text) =>
        Assert.Equal(RefusalKind.EmptyText, new TextCondition(text, TextMode.Contains).Validate()!.Kind);

    // Counterparty name

    [Theory]
    [InlineData(TextMode.Contains, "urad prace", "Úrad práce, sociálnych vecí a rodiny", true)]
    [InlineData(TextMode.EqualTo, "uniqa", "UNIQA", true)]
    [InlineData(TextMode.EqualTo, "uniqa", "Uniqa Životné Poistenie", false)]
    [InlineData(TextMode.Contains, "uniqa", "Generali", false)]
    public void A_name_condition_tests_the_counterparty_name(TextMode mode, string text, string name, bool expected) =>
        Assert.Equal(expected, new CounterpartyNameCondition(text, mode).Matches(Transfer(name: name)));

    [Fact]
    public void A_name_condition_never_matches_a_transaction_without_a_name()
    {
        var condition = new CounterpartyNameCondition("coop", TextMode.Contains);

        Assert.False(condition.Matches(Card("COOP Jednota")));
        Assert.False(condition.Matches(Transfer()));
    }

    [Fact]
    public void A_name_condition_needs_text() =>
        Assert.Equal(RefusalKind.EmptyText, new CounterpartyNameCondition(" ", TextMode.EqualTo).Validate()!.Kind);

    // Counterparty account

    [Theory]
    [InlineData("SK31 0200 0000 0012 3456 7890", "SK31 0200 0000 0012 3456 7890", true)]
    [InlineData("sk3102000000001234567890", "SK31 0200 0000 0012 3456 7890", true)]
    [InlineData("SK31 0200 0000 0012 3456 7891", "SK31 0200 0000 0012 3456 7890", false)]
    public void An_account_condition_compares_without_spaces_and_case(string given, string transactionAccount, bool expected) =>
        Assert.Equal(expected, new CounterpartyAccountCondition(given).Matches(Transfer(account: transactionAccount)));

    [Fact]
    public void An_account_condition_never_matches_a_transaction_without_an_account() =>
        Assert.False(new CounterpartyAccountCondition("SK31 0200").Matches(Card("COOP")));

    [Fact]
    public void An_account_condition_needs_an_account() =>
        Assert.Equal(RefusalKind.EmptyAccount, new CounterpartyAccountCondition("  ").Validate()!.Kind);

    // Amount

    [Theory]
    [InlineData(5, 15, -5, true)]
    [InlineData(5, 15, -15, true)]
    [InlineData(5, 15, -10, true)]
    [InlineData(5, 15, -4.99, false)]
    [InlineData(5, 15, -15.01, false)]
    [InlineData(5, 15, 10, true)]
    [InlineData(5, 15, 15.01, false)]
    public void An_amount_range_is_inclusive_and_ignores_direction(double min, double max, double amount, bool expected) =>
        Assert.Equal(expected, new AmountCondition((decimal)min, (decimal)max).Matches(Transfer(amount: (decimal)amount)));

    [Theory]
    [InlineData(500, -500, true)]
    [InlineData(500, -1200.50, true)]
    [InlineData(500, 1712.36, true)]
    [InlineData(500, -499.99, false)]
    public void A_lower_bound_alone_is_a_threshold(double min, double amount, bool expected) =>
        Assert.Equal(expected, new AmountCondition((decimal)min, null).Matches(Transfer(amount: (decimal)amount)));

    [Theory]
    [InlineData(20, -20, true)]
    [InlineData(20, 7, true)]
    [InlineData(20, 0, true)]
    [InlineData(20, -20.01, false)]
    public void An_upper_bound_alone_is_a_threshold(double max, double amount, bool expected) =>
        Assert.Equal(expected, new AmountCondition(null, (decimal)max).Matches(Transfer(amount: (decimal)amount)));

    [Fact]
    public void An_amount_condition_matches_money_in_and_out_of_the_same_size()
    {
        var condition = new AmountCondition(6.90m, 6.90m);

        Assert.True(condition.Matches(Fee(-6.90m)));
        Assert.True(condition.Matches(Fee(6.90m)));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(20, 10)]
    [InlineData(-5, 10)]
    [InlineData(5, -10)]
    public void An_amount_range_needs_a_bound_that_makes_sense(int? min, int? max) =>
        Assert.Equal(RefusalKind.InvalidAmountRange, new AmountCondition((decimal?)min, (decimal?)max).Validate()!.Kind);

    [Theory]
    [InlineData(10, 10)]
    [InlineData(0, 10)]
    [InlineData(5, null)]
    [InlineData(null, 5)]
    public void A_valid_amount_range_is_accepted(int? min, int? max) =>
        Assert.Null(new AmountCondition((decimal?)min, (decimal?)max).Validate());

    // Variable symbol

    [Theory]
    [InlineData("1234567", "1234567", true)]
    [InlineData("1234567", "12345678", false)]
    [InlineData("0138", "138", false)]
    public void A_variable_symbol_must_match_exactly(string given, string onTransaction, bool expected) =>
        Assert.Equal(expected, new VariableSymbolCondition(given).Matches(Transfer(variableSymbol: onTransaction)));

    [Fact]
    public void A_variable_symbol_never_matches_a_transaction_without_one() =>
        Assert.False(new VariableSymbolCondition("1234567").Matches(Transfer()));

    [Fact]
    public void A_variable_symbol_condition_needs_a_symbol() =>
        Assert.Equal(RefusalKind.EmptySymbol, new VariableSymbolCondition("").Validate()!.Kind);

    // Valid conditions are accepted

    [Fact]
    public void Valid_conditions_have_no_refusal()
    {
        Condition[] valid =
        [
            new TypeCondition([TransactionType.Transfer]),
            new TextCondition("netflix", TextMode.Contains),
            new CounterpartyNameCondition("uniqa", TextMode.EqualTo),
            new CounterpartyAccountCondition("SK31 0200"),
            new AmountCondition(1m, 2m),
            new VariableSymbolCondition("1"),
        ];

        Assert.All(valid, c => Assert.Null(c.Validate()));
    }

    [Fact]
    public void A_condition_is_the_same_whatever_the_order_of_its_types() =>
        Assert.Equal(
            new TypeCondition([TransactionType.Transfer, TransactionType.CardPayment]),
            new TypeCondition([TransactionType.CardPayment, TransactionType.Transfer, TransactionType.CardPayment]));
}
