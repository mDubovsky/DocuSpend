using System.Globalization;
using DocuSpend.SampleGenerator.Model;
using DocuSpend.SampleGenerator.Rendering;

namespace DocuSpend.SampleGenerator.Scenarios;

/// <summary>
/// Builders for the kinds of transactions seen on a real VÚB statement. All values are invented.
/// Create one per scenario: it numbers references, so a shared instance would make samples depend on build order.
/// </summary>
internal sealed class Tx
{
    /// <summary>The fee-type pictograms printed next to incoming payments.</summary>
    public const string FromOtherBank = "X";
    public const string OneOffElectronic = "E";
    public const string InstantIncoming = "1";

    private int _sequence;

    public Transaction Card(DateOnly booked, int daysEarlier, decimal amount, string merchant, string card, bool wallet = false) => new()
    {
        BookingDate = booked,
        ValueDate = booked.AddDays(-daysEarlier),
        Type = wallet ? TransactionType.WalletCardPayment : TransactionType.CardPayment,
        Amount = -amount,
        Card = card,
        Description = merchant,
        Reference = CardReference(booked),
    };

    public Transaction Incoming(DateOnly booked, decimal amount, string name, string account, string bic,
        string? purpose = null, string? vs = null, string? ks = null, string? ss = null, string? feeType = FromOtherBank,
        string? category = null, bool currencyLine = false, string? payerReference = null, DateOnly? valueDate = null) => new()
    {
        BookingDate = booked,
        ValueDate = valueDate ?? booked,
        Type = TransactionType.Transfer,
        Amount = amount,
        Description = SecondLine(purpose),
        CounterpartyName = name,
        CounterpartyAccount = account,
        CounterpartyBic = bic,
        VariableSymbol = vs,
        ConstantSymbol = ks,
        SpecificSymbol = ss,
        PayerReference = payerReference,
        PaymentPurpose = purpose,
        PurposeCategoryCode = category,
        Reference = TransferReference(booked),
        FeeType = feeType,
        ShowsCurrencyLine = currencyLine,
    };

    public Transaction Outgoing(DateOnly booked, decimal amount, string name, string account, string bic,
        string? purpose = null, string? vs = null, string? ks = null, string? ss = null, string? payerReference = null,
        DateOnly? valueDate = null) => new()
    {
        BookingDate = booked,
        ValueDate = valueDate ?? booked,
        Type = TransactionType.Transfer,
        Amount = -amount,
        Description = SecondLine(purpose),
        CounterpartyName = name,
        CounterpartyAccount = account,
        CounterpartyBic = bic,
        VariableSymbol = vs,
        ConstantSymbol = ks,
        SpecificSymbol = ss,
        PayerReference = payerReference,
        PaymentPurpose = purpose,
        Reference = TransferReference(booked),
    };

    /// <summary>A SEPA instant payment: the second line says so, and there is no purpose line.</summary>
    public Transaction Instant(DateOnly booked, decimal amount, string name, string account, string bic) => new()
    {
        BookingDate = booked,
        ValueDate = booked,
        Type = TransactionType.InstantTransfer,
        Amount = amount,
        Description = "SEPA Okamžitá platba",
        CounterpartyName = name,
        CounterpartyAccount = account,
        CounterpartyBic = bic,
        Reference = TransferReference(booked),
        FeeType = amount > 0 ? InstantIncoming : null,
    };

    /// <summary>The monthly account fee and its reversal, both booked on the last day.</summary>
    public Transaction[] AccountFeeReversed(DateOnly lastDay, decimal fee)
    {
        var vs = lastDay.ToString("MMyy", CultureInfo.InvariantCulture);
        var charged = new Transaction
        {
            BookingDate = lastDay,
            ValueDate = lastDay,
            Type = TransactionType.AccountFee,
            Amount = -fee,
            FeeAmount = -fee,
            Description = $"Vedenie konta {FictionalBank.ProductName}",
            VariableSymbol = vs,
            ConstantSymbol = "0558",
            Reference = TransferReference(lastDay),
        };
        return
        [
            charged,
            charged with
            {
                Type = TransactionType.FeeReversal,
                Amount = fee,
                FeeAmount = fee,
                Description = "Storno-vedenie konta",
                Reference = TransferReference(lastDay),
            },
        ];
    }

    /// <summary>
    /// A transfer's second line repeats its purpose. Assumption (not seen on the real statement): a long purpose is
    /// cut at the column edge there, and printed in full, wrapped, in the details block.
    /// </summary>
    private static string? SecondLine(string? purpose) =>
        purpose is { Length: > VubStatementRenderer.DescriptionWidth } ? purpose[..VubStatementRenderer.DescriptionWidth].TrimEnd() : purpose;

    private string CardReference(DateOnly booked) =>
        $"{booked:ddMM}22{++_sequence * 7919 % 10_000_000:0000000}";

    private string TransferReference(DateOnly booked)
    {
        const string alphabet = "ABCDEFGHJKLMNPRSTUVWXYZ";
        var n = ++_sequence * 104_729;
        var letters = string.Concat(Enumerable.Range(0, 5).Select(i => alphabet[(n >> (i * 3)) % alphabet.Length]));
        return $"{booked:yyMMdd}{n % 10}{letters}{alphabet[n % alphabet.Length]}";
    }
}
