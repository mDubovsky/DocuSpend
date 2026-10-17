using DocuSpend.SampleGenerator.Rendering;
using DocuSpend.Statements.Reading;
using UglyToad.PdfPig;

namespace DocuSpend.Statements.Tests;

/// <summary>Files that aren't readable VÚB statements are rejected with a clear reason (spec: Rejections).</summary>
public class RejectionTests
{
    private static readonly VubStatementReader Reader = new();

    [Theory]
    [InlineData("06-other-bank-statement", RejectionKind.NotAVubStatement)]
    [InlineData("07-not-a-statement", RejectionKind.NotAVubStatement)]
    [InlineData("08-corrupted-statement", RejectionKind.NotAPdf)]
    [InlineData("09-empty-file", RejectionKind.EmptyFile)]
    public void Invalid_samples_are_rejected_with_their_kind(string name, RejectionKind kind)
    {
        var result = Reader.Read(Samples.Pdf(name));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Statement);
        Assert.Equal(kind, result.Rejection!.Kind);
        Assert.Equal("reject", Samples.Record(name).Expectation);
    }

    [Theory]
    [InlineData("06-other-bank-statement")]
    [InlineData("07-not-a-statement")]
    [InlineData("08-corrupted-statement")]
    [InlineData("09-empty-file")]
    public void Rejection_messages_are_readable_and_quote_nothing_from_the_file(string name)
    {
        var message = Reader.Read(Samples.Pdf(name)).Rejection!.Message;
        // The samples' synthetic marking is added by the generator; it isn't statement content.
        var marking = FictionalBank.Footer.Split(' ').ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(message.Length > 15, message);
        Assert.EndsWith(".", message);
        foreach (var word in FileWords(Samples.Pdf(name)).Where(w => w.Length >= 4 && !marking.Contains(w)))
            Assert.DoesNotContain(word, message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("06-other-bank-statement")]
    [InlineData("07-not-a-statement")]
    public void Not_a_statement_says_which_part_was_missing(string name) =>
        Assert.Contains("title", Reader.Read(Samples.Pdf(name)).Rejection!.Message);

    [Fact]
    public void Each_rejection_kind_has_its_own_message()
    {
        var messages = Samples.Invalid.Select(n => Reader.Read(Samples.Pdf(n)).Rejection!)
            .GroupBy(r => r.Kind)
            .Select(g => g.First().Message)
            .ToList();

        Assert.Equal(messages.Count, messages.Distinct().Count());
    }

    [Fact]
    public void Random_bytes_are_not_a_pdf() =>
        Assert.Equal(RejectionKind.NotAPdf, Reader.Read("this is not a PDF"u8.ToArray()).Rejection!.Kind);

    private static IEnumerable<string> FileWords(byte[] pdf)
    {
        try
        {
            using var doc = PdfDocument.Open(pdf);
            return doc.GetPages().SelectMany(p => p.GetWords()).Select(w => w.Text).ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }
}
