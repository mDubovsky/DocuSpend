using System.Text.RegularExpressions;
using DocuSpend.Statements.Reading.Layout;

namespace DocuSpend.Statements.Reading.Sections;

/// <summary>The statement period, printed in the fee breakdown heading "(OD dd.mm.yyyy DO dd.mm.yyyy)".</summary>
internal static partial class PeriodSection
{
    public static (DateOnly Start, DateOnly End) Read(IEnumerable<Line> allLines)
    {
        foreach (var line in allLines)
        {
            var match = Heading().Match(line.Text);
            if (match.Success
                && VubText.FullDate(match.Groups[1].Value) is { } start
                && VubText.FullDate(match.Groups[2].Value) is { } end
                && start <= end)
            {
                return (start, end);
            }
        }
        throw new UnreadableException("The statement period could not be read: the fee breakdown heading with its dates is missing.");
    }

    [GeneratedRegex(@"\(OD (\S+) DO (\S+)\)")]
    private static partial Regex Heading();
}
