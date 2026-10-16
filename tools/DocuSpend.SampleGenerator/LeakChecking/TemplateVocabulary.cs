using DocuSpend.SampleGenerator.Model;
using DocuSpend.SampleGenerator.Rendering;
using DocuSpend.SampleGenerator.Scenarios;
using UglyToad.PdfPig;

namespace DocuSpend.SampleGenerator.LeakChecking;

/// <summary>
/// The words every VÚB-style statement shares (labels, bank boilerplate, transaction type texts), found by rendering
/// a probe statement whose values are all placeholders. These words are expected in both real and sample files.
/// </summary>
public static class TemplateVocabulary
{
    private const string P = "Q";

    public static IReadOnlyList<string> Words()
    {
        var t = new Tx();
        var day = new DateOnly(2000, 1, 15);
        var account = Iban.Slovak("0000", "000000", "0000000000");
        var probe = new StatementScenario
        {
            Name = "template-probe",
            AccountHolder = P,
            HolderAddress = [P, P],
            Iban = account,
            BranchAddress = P,
            PeriodStart = new DateOnly(2000, 1, 1),
            PeriodEnd = new DateOnly(2000, 1, 31),
            OpeningBalance = 0m,
            Transactions =
            [
                t.Card(day, 0, 0m, P, "0000"),
                t.Card(day, 0, 0m, P, "0000", wallet: true),
                t.Incoming(day, 0m, P, account, P, P, category: P, currencyLine: true),
                t.Instant(day, 0m, P, account, P),
                .. t.AccountFeeReversed(day, 0m),
            ],
        };

        using var doc = PdfDocument.Open(VubStatementRenderer.Render(probe));
        return doc.GetPages().SelectMany(p => p.GetWords()).Select(w => w.Text).Distinct().ToList();
    }
}
