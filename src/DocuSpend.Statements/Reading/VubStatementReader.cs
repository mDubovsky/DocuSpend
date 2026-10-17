using DocuSpend.Statements.Model;
using DocuSpend.Statements.Reading.Layout;
using DocuSpend.Statements.Reading.Sections;
using UglyToad.PdfPig;

namespace DocuSpend.Statements.Reading;

/// <summary>
/// Reads monthly statements in the VÚB layout. The layout is recognized by its structure (title, account block,
/// summary, table header), never by the bank's name or other identity; see docs/decisions/0002.
/// </summary>
public sealed class VubStatementReader : IStatementReader
{
    public ReadResult Read(byte[] file)
    {
        if (file.Length == 0)
            return ReadResult.Reject(RejectionKind.EmptyFile, "The file is empty.");

        IReadOnlyList<IReadOnlyList<Word>> pages;
        try
        {
            using var doc = PdfDocument.Open(file);
            pages = PageText.Words(doc);
        }
        catch (Exception)
        {
            return ReadResult.Reject(RejectionKind.NotAPdf,
                "The file isn't a readable PDF. It may be damaged, incomplete or password-protected.");
        }

        var lines = pages.Select(PageText.Lines).ToList();
        if (Recognize(lines) is { } missing)
            return ReadResult.Reject(RejectionKind.NotAVubStatement,
                $"This doesn't look like a VÚB monthly statement: {missing} wasn't found.");

        Statement statement;
        try
        {
            statement = ReadStatement(lines);
        }
        catch (UnreadableException e)
        {
            return ReadResult.Reject(RejectionKind.Unreadable, e.Message);
        }

        return Reconciliation.Check(statement) is { } disagreement
            ? ReadResult.Reject(RejectionKind.DoesNotReconcile, disagreement)
            : ReadResult.Success(statement);
    }

    private static Statement ReadStatement(IReadOnlyList<IReadOnlyList<Line>> pages)
    {
        var page1 = pages[0];
        var tableTop = page1.First(l => l.Contains("Zaúčt.", "Valuta")).Top;
        var aboveTable = page1.Where(l => l.Top < tableTop).ToList();

        var header = HeaderSection.Read(aboveTable);
        var summary = SummarySection.Read(aboveTable);
        var (start, end) = PeriodSection.Read(pages.SelectMany(p => p));

        return new Statement
        {
            AccountHolder = header.AccountHolder,
            Iban = header.Iban,
            Bic = header.Bic,
            Currency = header.Currency,
            PeriodStart = start,
            PeriodEnd = end,
            OpeningBalance = summary.Opening,
            ClosingBalance = summary.Closing,
            TotalDeposits = summary.Deposits,
            TotalWithdrawals = summary.Withdrawals,
            TotalFees = summary.Fees,
            Transactions = TransactionSection.Read(pages, start, end, header.Currency),
        };
    }

    /// <summary>The first expected part of the layout that is missing, or null when the layout is recognized.</summary>
    private static string? Recognize(IReadOnlyList<IReadOnlyList<Line>> pages)
    {
        if (pages.Count == 0)
            return "any page";
        var first = pages[0];
        if (!first.Any(l => l.Contains("VÝPIS", "Z", "ÚČTU")))
            return "the statement title";
        // The balance summary isn't required here: a statement without it is recognized, then rejected as unreadable.
        if (!first.Any(l => l.StartingAt(VubLayout.AccountLabelX)?.Text == "Číslo:"))
            return "the account block";
        if (!first.Any(l => l.Contains("Zaúčt.", "Valuta")))
            return "the transaction table";
        return null;
    }
}
