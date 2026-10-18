using DocuSpend.Statements.Model;

namespace DocuSpend.Categorization.Tests;

/// <summary>Builds transactions for tests. Everything not given is absent, as on a statement that doesn't show it.</summary>
internal static class TestTransactions
{
    private static readonly DateOnly Day = new(2026, 3, 10);

    public static Transaction Card(string description, decimal amount = -10m, bool wallet = false) => new()
    {
        BookingDate = Day,
        ValueDate = Day,
        Type = wallet ? TransactionType.WalletCardPayment : TransactionType.CardPayment,
        Amount = amount,
        Currency = "EUR",
        Card = "1234",
        Description = description,
    };

    public static Transaction Transfer(
        decimal amount = -10m,
        string? description = null,
        string? purpose = null,
        string? name = null,
        string? account = null,
        string? variableSymbol = null,
        TransactionType type = TransactionType.Transfer) => new()
    {
        BookingDate = Day,
        ValueDate = Day,
        Type = type,
        Amount = amount,
        Currency = "EUR",
        Description = description,
        PaymentPurpose = purpose,
        CounterpartyName = name,
        CounterpartyAccount = account,
        VariableSymbol = variableSymbol,
    };

    public static Transaction Fee(decimal amount) => new()
    {
        BookingDate = Day,
        ValueDate = Day,
        Type = amount < 0 ? TransactionType.AccountFee : TransactionType.FeeReversal,
        Amount = amount,
        Currency = "EUR",
        FeeAmount = amount,
        Description = "Vedenie konta",
    };
}
