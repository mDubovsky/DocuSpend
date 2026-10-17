using DocuSpend.Statements.Reading.Layout;

namespace DocuSpend.Statements.Reading.Sections;

internal sealed record Summary(decimal Opening, decimal Deposits, decimal Withdrawals, decimal Fees, decimal Closing);

/// <summary>
/// The balance summary on page 1. Each amount is right-aligned in the summary column and printed a few points
/// below its label, so it is found by position relative to the label, not on the label's line.
/// </summary>
internal static class SummarySection
{
    public static Summary Read(IReadOnlyList<Line> aboveTable) => new(
        Amount(aboveTable, ["Vlastné", "použiteľné", "prostriedky"], "the opening balance"),
        Amount(aboveTable, ["Vklady", "spolu"], "the total deposits"),
        Amount(aboveTable, ["Výbery", "spolu"], "the total withdrawals"),
        Amount(aboveTable, ["Poplatky", "spolu"], "the total fees"),
        Amount(aboveTable, ["Vlast.", "použit.", "prostr."], "the closing balance"));

    private static decimal Amount(IReadOnlyList<Line> lines, string[] label, string what)
    {
        var labelLine = lines.FirstOrDefault(l => l.StartingAt(VubLayout.SummaryLabelX)?.Text == label[0] && l.Contains(label))
            ?? throw new UnreadableException($"The balance summary could not be read: the label for {what} is missing.");

        // Anchor on the label word: the line can also hold taller text further right (the address block).
        var labelTop = labelLine.StartingAt(VubLayout.SummaryLabelX)!.Top;
        var value = lines
            .SelectMany(l => l.Words)
            .Where(w => Math.Abs(w.Right - VubLayout.SummaryValueRight) <= VubLayout.ColumnTolerance && VubText.IsAmount(w.Text))
            .Where(w => w.Top >= labelTop - VubLayout.LineTolerance && w.Top <= labelTop + VubLayout.SummaryValueMaxDrop)
            .OrderBy(w => w.Top)
            .FirstOrDefault();

        return VubText.Amount(value?.Text)
            ?? throw new UnreadableException($"The balance summary could not be read: {what} is missing or not an amount.");
    }
}
