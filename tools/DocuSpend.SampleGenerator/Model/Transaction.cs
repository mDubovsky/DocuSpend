namespace DocuSpend.SampleGenerator.Model;

public enum TransactionType
{
    CardPayment,
    WalletCardPayment,
    Transfer,
    InstantTransfer,
    AccountFee,
    FeeReversal,
    CashWithdrawal,

    /// <summary>A type the reader doesn't know; <see cref="Transaction.PrintedType"/> says what the row prints.</summary>
    Unknown,
}

/// <summary>One transaction as it appears on a statement. See work/001-synthetic-statements/layout.md.</summary>
public sealed record Transaction
{
    public required DateOnly BookingDate { get; init; }
    public required DateOnly ValueDate { get; init; }
    public required TransactionType Type { get; init; }

    /// <summary>Effect on the balance: negative for money going out.</summary>
    public required decimal Amount { get; init; }

    public string Currency { get; init; } = "EUR";

    /// <summary>Last 4 digits of the card, for card payments.</summary>
    public string? Card { get; init; }

    /// <summary>Second-line text: merchant for cards, message or type for transfers, fee name for fees.</summary>
    public string? Description { get; init; }

    public string? CounterpartyName { get; init; }
    public string? CounterpartyAccount { get; init; }
    public string? CounterpartyBic { get; init; }
    public string? VariableSymbol { get; init; }
    public string? ConstantSymbol { get; init; }
    public string? SpecificSymbol { get; init; }
    public string? PayerReference { get; init; }
    public string? PaymentPurpose { get; init; }
    public string? PurposeCategoryCode { get; init; }
    public string? Reference { get; init; }

    /// <summary>Fee-type pictogram shown in the "Typ poplatku" column.</summary>
    public string? FeeType { get; init; }

    /// <summary>Amount printed in the "Poplatok" column, for fee rows.</summary>
    public decimal? FeeAmount { get; init; }

    /// <summary>For <see cref="TransactionType.Unknown"/>: the type text the row prints, e.g. "PLATBA HODINKAMI".</summary>
    public string? PrintedType { get; init; }

    /// <summary>Render-only: print the "Mena/suma/kurz:" line in the details block.</summary>
    public bool ShowsCurrencyLine { get; init; }

    /// <summary>Render-only: a wallet card payment made with Google Pay ("G-PAY") rather than Apple Pay ("A-PAY").</summary>
    public bool GooglePay { get; init; }

    public bool IsFee => Type is TransactionType.AccountFee or TransactionType.FeeReversal;

    public bool IsCard => Type is TransactionType.CardPayment or TransactionType.WalletCardPayment;

    public bool IsTransfer => Type is TransactionType.Transfer or TransactionType.InstantTransfer;

    /// <summary>Printed like a card payment: card number on the main line, value date on the second.</summary>
    public bool IsCardRow => IsCard || Type == TransactionType.CashWithdrawal || Type == TransactionType.Unknown && Card is not null;

    /// <summary>Has a details block: transfers, and unknown types printed without a card.</summary>
    public bool HasDetails => IsTransfer || Type == TransactionType.Unknown && Card is null;
}
