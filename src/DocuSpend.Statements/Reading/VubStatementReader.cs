using DocuSpend.Statements.Reading.Layout;
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

        return ReadResult.Reject(RejectionKind.Unreadable, "Reading statements is not finished yet.");
    }

    /// <summary>The first expected part of the layout that is missing, or null when the layout is recognized.</summary>
    private static string? Recognize(IReadOnlyList<IReadOnlyList<Line>> pages)
    {
        if (pages.Count == 0)
            return "any page";
        var first = pages[0];
        if (!first.Any(l => l.Contains("VÝPIS", "Z", "ÚČTU")))
            return "the statement title";
        if (!first.Any(l => l.StartingAt(VubLayout.AccountLabelX)?.Text == "Číslo:"))
            return "the account block";
        if (!first.Any(l => l.Contains("Vklady", "spolu")))
            return "the balance summary";
        if (!first.Any(l => l.Contains("Zaúčt.", "Valuta")))
            return "the transaction table";
        return null;
    }
}
