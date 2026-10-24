using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using DocuSpend.SampleGenerator.Rendering;
using DocuSpend.SampleGenerator.Scenarios;

namespace DocuSpend.SampleGenerator.Tests;

/// <summary>
/// The canonical form ignores only how streams are compressed, and nothing else, so comparing it is as strict as
/// comparing bytes about what the PDF contains.
/// </summary>
[Trait("Check", "privacy-no-real-data")]
public class PdfCanonicalTests
{
    private static readonly byte[] Pdf = VubStatementRenderer.Render(ValidSamples.TypicalMonth());

    [Fact]
    public void Compressed_streams_are_decompressed()
    {
        Assert.Contains("[inflated:", PdfCanonical.Form(Pdf));
    }

    [Theory]
    [InlineData(CompressionLevel.NoCompression)]
    [InlineData(CompressionLevel.SmallestSize)]
    [InlineData(CompressionLevel.Fastest)]
    public void Compressing_a_stream_differently_gives_the_same_form(CompressionLevel level)
    {
        var recompressed = RewriteFirstStream(Pdf, content => content, level);

        Assert.NotEqual(Pdf, recompressed);
        Assert.Equal(PdfCanonical.Form(Pdf), PdfCanonical.Form(recompressed));
    }

    [Fact]
    public void Platform_line_endings_inside_a_stream_give_the_same_form()
    {
        var windows = RewriteFirstStream(Pdf, content => Encoding.Latin1.GetBytes(
            Encoding.Latin1.GetString(content).Replace("\r\n", "\n").Replace("\n", "\r\n")), CompressionLevel.Optimal);
        var linux = RewriteFirstStream(Pdf, content => Encoding.Latin1.GetBytes(
            Encoding.Latin1.GetString(content).Replace("\r\n", "\n")), CompressionLevel.Optimal);

        Assert.NotEqual(windows, linux);
        Assert.Equal(PdfCanonical.Form(windows), PdfCanonical.Form(linux));
    }

    [Fact]
    public void A_change_inside_a_compressed_stream_gives_a_different_form()
    {
        var changed = RewriteFirstStream(Pdf, content => { content[^2] ^= 1; return content; }, CompressionLevel.Optimal);

        Assert.NotEqual(PdfCanonical.Form(Pdf), PdfCanonical.Form(changed));
    }

    [Fact]
    public void A_change_outside_the_streams_gives_a_different_form()
    {
        var text = Encoding.Latin1.GetString(Pdf);
        var changed = Encoding.Latin1.GetBytes(text.Replace("/Producer", "/Producex"));

        Assert.NotEqual(Pdf, changed);
        Assert.NotEqual(PdfCanonical.Form(Pdf), PdfCanonical.Form(changed));
    }

    /// <summary>Decompresses the first compressed stream, changes it, and compresses it again at the given level.</summary>
    private static byte[] RewriteFirstStream(byte[] pdf, Func<byte[], byte[]> change, CompressionLevel level)
    {
        var text = Encoding.Latin1.GetString(pdf);
        var start = Regex.Match(text, @"/FlateDecode[^>]*/Length (\d+)>>\s*stream\r?\n|/Length (\d+)/Filter/FlateDecode>>\s*stream\r?\n");
        Assert.True(start.Success, "No compressed stream with a direct length found.");
        var lengthGroup = start.Groups[1].Success ? start.Groups[1] : start.Groups[2];
        var length = int.Parse(lengthGroup.Value);
        var dataStart = start.Index + start.Length;

        byte[] content;
        using (var input = new ZLibStream(new MemoryStream(pdf, dataStart, length), CompressionMode.Decompress))
        using (var buffer = new MemoryStream())
        {
            input.CopyTo(buffer);
            content = change(buffer.ToArray());
        }
        byte[] compressed;
        using (var buffer = new MemoryStream())
        {
            using (var output = new ZLibStream(buffer, level))
                output.Write(content);
            compressed = buffer.ToArray();
        }

        var before = text[..lengthGroup.Index] + compressed.Length + text[(lengthGroup.Index + lengthGroup.Length)..dataStart];
        return [.. Encoding.Latin1.GetBytes(before), .. compressed, .. pdf.AsSpan(dataStart + length).ToArray()];
    }
}
