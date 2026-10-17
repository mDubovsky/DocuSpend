namespace DocuSpend.Statements.Model;

public enum TransactionType
{
    CardPayment,
    WalletCardPayment,
    Transfer,
    InstantTransfer,
    AccountFee,
    FeeReversal,
}

/// <summary>One transaction as a statement prints it. A field the statement doesn't show is <c>null</c>.</summary>
public sealed record Transaction
{
    public required DateOnly BookingDate { get; init; }
    public required DateOnly ValueDate { get; init; }
    public required TransactionType Type { get; init; }

    /// <summary>Effect on the balance, exact to the cent: negative for money going out.</summary>
    public required decimal Amount { get; init; }

    public required string Currency { get; init; }

    /// <summary>Last 4 digits of the card, for card payments.</summary>
    public string? Card { get; init; }

    /// <summary>The transaction's second line as printed: merchant, message, type of transfer or fee name.</summary>
    public string? Description { get; init; }

    public string? CounterpartyName { get; init; }
    public string? CounterpartyAccount { get; init; }
    public string? CounterpartyBic { get; init; }
    public string? VariableSymbol { get; init; }
    public string? ConstantSymbol { get; init; }
    public string? SpecificSymbol { get; init; }
    public string? PayerReference { get; init; }

    /// <summary>The full payment purpose from the details block.</summary>
    public string? PaymentPurpose { get; init; }

    public string? PurposeCategoryCode { get; init; }
    public string? Reference { get; init; }

    /// <summary>The fee-type pictogram printed next to the transaction.</summary>
    public string? FeeType { get; init; }

    /// <summary>For fee rows: the amount printed in the fee column, equal to <see cref="Amount"/>.</summary>
    public decimal? FeeAmount { get; init; }
}
