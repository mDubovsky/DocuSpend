using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>Showing cash withdrawals and unknown types (work/041-unknown-transaction-kinds/spec.md: Showing it).</summary>
public class UnknownTypesTests
{
    private static Transaction Tx(TransactionType type, decimal amount, string? description, string? printedType = null) => new()
    {
        BookingDate = new DateOnly(2026, 4, 10),
        ValueDate = new DateOnly(2026, 4, 10),
        Type = type,
        Amount = amount,
        Currency = "EUR",
        Description = description,
        PrintedType = printedType,
    };

    private static Statement StatementOf(params Transaction[] transactions)
    {
        var withdrawals = transactions.Where(t => t.Amount < 0).Sum(t => t.Amount);
        var deposits = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        return new Statement
        {
            AccountHolder = "TEST",
            Iban = "SK00",
            Bic = "TESTSKXX",
            Currency = "EUR",
            PeriodStart = new DateOnly(2026, 4, 1),
            PeriodEnd = new DateOnly(2026, 4, 30),
            OpeningBalance = 500m,
            ClosingBalance = 500m + deposits + withdrawals,
            TotalDeposits = deposits,
            TotalWithdrawals = withdrawals,
            TotalFees = 0m,
            Transactions = transactions,
        };
    }

    private static StatementViewModel Show(Statement statement)
    {
        var view = new StatementViewModel(CategorizationConfig.Default);
        view.Show(statement);
        return view;
    }

    private static Transaction Unknown(string printed = "PLATBA HODINKAMI") => Tx(TransactionType.Unknown, -2m, null, printed);

    // The note

    [Theory]
    [InlineData(1, "1 transakcia neznámeho typu")]
    [InlineData(2, "2 transakcie neznámeho typu")]
    [InlineData(3, "3 transakcie neznámeho typu")]
    [InlineData(4, "4 transakcie neznámeho typu")]
    [InlineData(5, "5 transakcií neznámeho typu")]
    [InlineData(21, "21 transakcií neznámeho typu")]
    public void The_note_counts_the_unknown_types_in_slovak(int count, string note)
    {
        Assert.Equal(note, Texts.UnknownTypes(count));
        Assert.Equal(note, Show(StatementOf([.. Enumerable.Range(0, count).Select(_ => Unknown())])).UnknownTypes);
    }

    [Fact]
    public void A_statement_without_unknown_types_has_no_note()
    {
        var view = Show(Samples.Read("01-typical-month"));

        Assert.Null(view.UnknownTypes);
        Assert.Null(Show(StatementOf(Tx(TransactionType.CashWithdrawal, -50m, "ATM NITRA"))).UnknownTypes);
    }

    [Fact]
    public void Sample_ten_notes_its_two_unknown_types() =>
        Assert.Equal("2 transakcie neznámeho typu", Show(Samples.Read("10-new-types-month")).UnknownTypes);

    [Fact]
    public void The_note_is_counted_even_when_a_rule_places_the_unknown_type()
    {
        var config = CategorizationConfig.Default.AddUserRuleOnTop(new TextCondition("hodinkami", TextMode.Contains), new SubcategoryId("leisure.other")).Value!;
        var view = new StatementViewModel(config);
        view.Show(StatementOf(Unknown()));

        Assert.Equal("1 transakcia neznámeho typu", view.UnknownTypes);
    }

    [Fact]
    public void The_help_mark_explains_the_note() =>
        Assert.Equal(
            "DocuSpend tento typ transakcie zatiaľ nepozná. Transakciu načítal so sumou aj dátumom a v zozname transakcií má typ uvedený pred popisom. Zaradiť ju môžete pravidlom s textom.",
            Texts.UnknownTypesHelp);

    // The transaction list

    [Fact]
    public void An_unknown_type_and_a_cash_withdrawal_show_their_type_before_the_description()
    {
        var view = Show(StatementOf(
            Tx(TransactionType.Unknown, -3m, "SPRAVA BYTOV ZOBOR", "ZRÁŽKA ZO ZÁLOHY"),
            Unknown("PLATBA HODINKAMI"),
            Tx(TransactionType.CashWithdrawal, -50m, "ATM00417 NITRA"),
            Tx(TransactionType.CardPayment, -4m, "OBCHOD NA ROHU")));
        view.SelectedCategory = new CategoryId("uncategorized-out");

        Assert.Equal(
            ["ZRÁŽKA ZO ZÁLOHY · SPRAVA BYTOV ZOBOR", "PLATBA HODINKAMI", "Výber hotovosti · ATM00417 NITRA", "OBCHOD NA ROHU"],
            view.Transactions.Select(t => t.Description));
    }

    [Fact]
    public void A_cash_withdrawal_without_a_description_shows_its_type_alone()
    {
        var view = Show(StatementOf(Tx(TransactionType.CashWithdrawal, -50m, null)));
        view.SelectedCategory = new CategoryId("uncategorized-out");

        Assert.Equal("Výber hotovosti", view.Transactions.Single().Description);
    }

    // Rules

    [Fact]
    public void A_type_rule_for_cash_withdrawals_reads_in_slovak() =>
        Assert.Equal("Typ: Výber hotovosti", MainViewModel.Describe(new TypeCondition([TransactionType.CashWithdrawal])));
}

/// <summary>A rule on cash withdrawals is kept between sessions like any other.</summary>
public sealed class CashWithdrawalRuleKeptTests : IDisposable
{
    private readonly string _folder = Path.Join(Path.GetTempPath(), "docuspend-cash-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void A_cash_withdrawal_rule_is_back_after_a_restart_and_still_applies()
    {
        var file = Path.Join(_folder, "rules.json");
        var config = CategorizationConfig.Default
            .AddUserRuleOnTop(new TypeCondition([TransactionType.CashWithdrawal]), new SubcategoryId("other-expenses.other")).Value!;
        Assert.True(new RuleStore(file).Save(config));

        var load = new RuleStore(file).Load(CategorizationConfig.Default);
        var placed = Categorizer.Categorize(Samples.Read("10-new-types-month"), load.Config).Transactions
            .Where(t => t.Transaction.Type == TransactionType.CashWithdrawal);

        Assert.Equal(RulesProblem.None, load.Problem);
        Assert.Equal(config.UserRules, load.Config.UserRules);
        Assert.All(placed, t => Assert.Equal("other-expenses.other", t.Placement.Subcategory!.Value.Value));
    }
}
