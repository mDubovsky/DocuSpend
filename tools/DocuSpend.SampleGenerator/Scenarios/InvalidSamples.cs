using DocuSpend.SampleGenerator.Records;
using DocuSpend.SampleGenerator.Rendering;

namespace DocuSpend.SampleGenerator.Scenarios;

/// <summary>A file a reader must reject as not a readable VÚB statement.</summary>
public sealed record InvalidSample(string Name, byte[] Content, string Reason)
{
    public ExpectedRecord Record => ExpectedRecord.ForInvalid(Name, Reason);
}

/// <summary>The invalid samples from the spec (6 to 9).</summary>
public static class InvalidSamples
{
    public static IReadOnlyList<InvalidSample> All() =>
    [
        new("06-other-bank-statement", OtherDocuments.OtherBankStatement(),
            "A statement from a different (invented) bank, with a different layout and identity."),
        new("07-not-a-statement", OtherDocuments.Invoice(),
            "A valid PDF that is not a bank statement (an invoice)."),
        new("08-corrupted-statement", Truncated(VubStatementRenderer.Render(ValidSamples.TypicalMonth())),
            "The typical-month statement cut off partway: no cross-reference table, trailer or end-of-file marker."),
        new("09-empty-file", [], "A zero-byte file with a .pdf name."),
    ];

    /// <summary>The first 40% of the file, so the PDF's structure at the end is missing.</summary>
    public static byte[] Truncated(byte[] pdf) => pdf[..(pdf.Length * 2 / 5)];
}
