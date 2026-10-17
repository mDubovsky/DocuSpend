using DocuSpend.Statements.Reading.Layout;

namespace DocuSpend.Statements.Reading.Sections;

internal sealed record Header(string AccountHolder, string Iban, string Bic, string Currency);

/// <summary>The account block on page 1: labels at the left, values in bold to their right.</summary>
internal static class HeaderSection
{
    public static Header Read(IReadOnlyList<Line> aboveTable) => new(
        Value(aboveTable, "Názov:", "the account holder"),
        Value(aboveTable, "Číslo:", "the account number"),
        Value(aboveTable, "BIC:", "the BIC"),
        Value(aboveTable, "Mena:", "the currency"));

    private static string Value(IReadOnlyList<Line> lines, string label, string what)
    {
        var line = lines.FirstOrDefault(l => l.StartingAt(VubLayout.AccountLabelX)?.Text == label)
            ?? throw new UnreadableException($"The account block could not be read: the label for {what} is missing.");
        return line.TextBetween(VubLayout.AccountValueX, VubLayout.AccountValueEnd)
            ?? throw new UnreadableException($"The account block could not be read: {what} is missing.");
    }
}
