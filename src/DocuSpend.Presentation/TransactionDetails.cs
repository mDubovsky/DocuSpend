using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation;

/// <summary>One line of an open row: a label and what the statement says.</summary>
/// <param name="StartsGroup">The first line of a group after another group, which the row sets apart with a little space.</param>
public sealed record TransactionDetail(string Label, string Value, bool StartsGroup);

/// <summary>
/// What an open row shows about its transaction (work/045-transaction-details/spec.md: what the details show): every
/// detail the reader read, in a fixed order, grouped by what the user asks first, leaving out what it doesn't have.
/// </summary>
public static class TransactionDetails
{
    public static IReadOnlyList<TransactionDetail> Of(Transaction t)
    {
        (string Label, string? Value)[][] groups =
        [
            // Who
            [
                (Texts.DetailCounterpartyName, t.CounterpartyName),
                (Texts.DetailCounterpartyAccount, t.CounterpartyAccount),
                (Texts.DetailCounterpartyBic, t.CounterpartyBic),
            ],
            // What for. The purpose is often the description word for word; it shows only when it says more.
            [
                (Texts.DetailDescription, t.Description),
                (Texts.DetailPaymentPurpose, t.PaymentPurpose == t.Description ? null : t.PaymentPurpose),
                (Texts.DetailVariableSymbol, t.VariableSymbol),
                (Texts.DetailConstantSymbol, t.ConstantSymbol),
                (Texts.DetailSpecificSymbol, t.SpecificSymbol),
                (Texts.DetailPayerReference, t.PayerReference),
            ],
            // How
            [
                (Texts.DetailType, t.Type == TransactionType.Unknown ? t.PrintedType : Texts.TypeName(t.Type)),
                (Texts.DetailCard, string.IsNullOrWhiteSpace(t.Card) ? null : Texts.CardEnding(t.Card)),
            ],
            // When
            [
                (Texts.DetailBookingDate, SlovakFormat.Date(t.BookingDate)),
                (Texts.DetailValueDate, SlovakFormat.Date(t.ValueDate)),
            ],
            // The bank's own codes, as printed
            [
                (Texts.DetailPurposeCategoryCode, t.PurposeCategoryCode),
                (Texts.DetailFeeType, t.FeeType),
                (Texts.DetailReference, t.Reference),
            ],
        ];

        var details = new List<TransactionDetail>();
        foreach (var group in groups)
        {
            var first = true;
            foreach (var (label, value) in group)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                details.Add(new TransactionDetail(label, value, StartsGroup: first && details.Count > 0));
                first = false;
            }
        }
        return details;
    }
}
