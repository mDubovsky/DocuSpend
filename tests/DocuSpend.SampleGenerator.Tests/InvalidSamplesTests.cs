using System.Text;
using DocuSpend.SampleGenerator.Records;
using DocuSpend.SampleGenerator.Rendering;
using DocuSpend.SampleGenerator.Scenarios;

namespace DocuSpend.SampleGenerator.Tests;

/// <summary>Checks the invalid samples (6 to 9) against work/001-synthetic-statements/spec.md.</summary>
public class InvalidSamplesTests
{
    private static readonly Lazy<Dictionary<string, InvalidSample>> Samples = new(() =>
        InvalidSamples.All().ToDictionary(s => s.Name));

    private static byte[] Content(string name) => Samples.Value[name].Content;

    /// <summary>Text that identifies a statement in the VÚB layout: its title and table header, and the samples' bank.</summary>
    private static readonly string[] VubMarkers = ["VÝPIS", "Zaúčt.", "Realiz.", FictionalBank.Bic, "Vzorová"];

    [Fact]
    public void The_four_invalid_samples_exist_with_their_spec_names() =>
        Assert.Equal(
            ["06-other-bank-statement", "07-not-a-statement", "08-corrupted-statement", "09-empty-file"],
            InvalidSamples.All().Select(s => s.Name));

    [Theory]
    [InlineData("06-other-bank-statement")]
    [InlineData("07-not-a-statement")]
    public void Readable_pdfs_carry_no_vub_identity(string name)
    {
        var words = PdfText.Words(Content(name)).Select(w => w.Text).ToList();

        Assert.NotEmpty(words);
        Assert.DoesNotContain(words, w => VubMarkers.Any(m => w.Contains(m, StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("06-other-bank-statement")]
    [InlineData("07-not-a-statement")]
    public void Readable_pdfs_are_marked_synthetic(string name)
    {
        var pdf = Content(name);

        Assert.All(PdfText.MarginTexts(pdf), t => Assert.Contains(VubStatementRenderer.SyntheticMarking, t));
        Assert.Equal(VubStatementRenderer.SyntheticMarking, PdfText.Info(pdf)["Title"]);
    }

    [Fact]
    public void Other_bank_statement_looks_like_a_statement()
    {
        var words = PdfText.Words(Content("06-other-bank-statement")).Select(w => w.Text).ToList();

        Assert.Contains("Zostatok", words);
        Assert.Contains(words, w => w.Contains(','));
    }

    [Fact]
    public void Non_statement_is_an_invoice()
    {
        var words = PdfText.Words(Content("07-not-a-statement")).Select(w => w.Text).ToList();

        Assert.Contains("FAKTÚRA", words);
    }

    [Fact]
    public void Corrupted_statement_is_a_damaged_vub_statement()
    {
        var corrupted = Content("08-corrupted-statement");
        var original = VubStatementRenderer.Render(ValidSamples.TypicalMonth());

        Assert.StartsWith("%PDF-", Encoding.Latin1.GetString(corrupted, 0, 5));
        Assert.True(corrupted.Length < original.Length / 2);
        Assert.Equal(original.AsSpan(0, corrupted.Length).ToArray(), corrupted);
        Assert.DoesNotContain("%%EOF", Encoding.Latin1.GetString(corrupted));
    }

    [Fact]
    public void Empty_file_has_no_bytes() =>
        Assert.Empty(Content("09-empty-file"));

    [Theory]
    [InlineData("08-corrupted-statement")]
    [InlineData("09-empty-file")]
    public void A_pdf_library_refuses_to_open_broken_files(string name) =>
        Assert.ThrowsAny<Exception>(() => UglyToad.PdfPig.PdfDocument.Open(Content(name)).Dispose());

    [Fact]
    public void Each_invalid_sample_has_a_reject_record()
    {
        foreach (var sample in InvalidSamples.All())
        {
            var record = RecordWriter.Deserialize(RecordWriter.Serialize(sample.Record));

            Assert.Equal(sample.Name, record.Sample);
            Assert.Equal("reject", record.Expectation);
            Assert.False(string.IsNullOrWhiteSpace(record.Reason));
            Assert.Null(record.Statement);
        }
    }

    [Fact]
    public void Invalid_samples_are_byte_identical_across_runs()
    {
        var first = InvalidSamples.All();
        var second = InvalidSamples.All();

        Assert.All(first.Zip(second), pair => Assert.Equal(pair.First.Content, pair.Second.Content));
    }
}
