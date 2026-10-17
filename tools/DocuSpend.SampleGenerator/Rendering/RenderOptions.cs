using DocuSpend.SampleGenerator.Model;

namespace DocuSpend.SampleGenerator.Rendering;

/// <summary>
/// Test-only ways to render a statement that is inconsistent or damaged, for testing a reader's rejections.
/// The committed samples never use them; a PDF rendered with them has no valid record.
/// </summary>
public sealed record RenderOptions
{
    /// <summary>Totals to print in the balance summary instead of those computed from the transactions.</summary>
    public StatementTotals? PrintedTotals { get; init; }

    /// <summary>
    /// Applied to every piece of statement text before it is drawn; return <c>null</c> to leave it out.
    /// The synthetic marking (margin and footer) is never transformed.
    /// </summary>
    public Func<string, string?>? TextTransform { get; init; }
}
