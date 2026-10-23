using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace DocuSpend.SampleGenerator.Tests;

/// <summary>
/// A PDF with its compressed streams decompressed, so two files can be compared exactly regardless of how the
/// compressor encoded them. PDFsharp compresses with .NET's DeflateStream, whose output differs between platforms
/// (the committed samples are made on Windows, CI also runs on Linux) while the decompressed content is identical.
/// Everything else is kept byte for byte, except what follows from the compressed sizes (stream /Length values and
/// the cross-reference offsets) and line endings: PDFsharp ends some lines with the platform's own (\r\n on
/// Windows, \n on Linux), for example in the font's character map, so \r\n counts as \n.
/// </summary>
public static partial class PdfCanonical
{
    public static string Form(byte[] pdf)
    {
        // Latin-1 maps every byte to one char and back, so binary content survives.
        var text = Encoding.Latin1.GetString(pdf);
        var result = new StringBuilder();
        var at = 0;
        foreach (Match start in StreamStart().Matches(text))
        {
            if (start.Index < at)
                continue;
            var dictionary = text[text.LastIndexOf(" obj", start.Index, StringComparison.Ordinal)..start.Index];
            var length = int.Parse(Length().Match(dictionary).Groups[1].Value);
            var dataStart = start.Index + start.Length;
            var data = pdf.AsSpan(dataStart, length).ToArray();

            result.Append(Mask(text[at..dataStart]));
            result.Append(dictionary.Contains("/FlateDecode", StringComparison.Ordinal)
                ? $"[inflated:{Content(Inflate(data))}]"
                : $"[raw:{Content(data)}]");
            at = dataStart + length;
        }
        result.Append(Mask(text[at..]));
        return result.ToString();
    }

    /// <summary>A stream's content with line endings made uniform, as Latin-1 text so a difference is readable.</summary>
    private static string Content(byte[] data) => Encoding.Latin1.GetString(data).Replace("\r\n", "\n");

    private static string Mask(string text)
    {
        text = text.Replace("\r\n", "\n");
        text = Length().Replace(text, "/Length ?");
        text = CrossReference().Replace(text, "xref ? trailer");
        return StartXref().Replace(text, "startxref ?");
    }

    private static byte[] Inflate(byte[] data)
    {
        using var input = new ZLibStream(new MemoryStream(data), CompressionMode.Decompress);
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }

    [GeneratedRegex(@"(?<=>>\s*)stream\r?\n")]
    private static partial Regex StreamStart();

    [GeneratedRegex(@"/Length (\d+)")]
    private static partial Regex Length();

    [GeneratedRegex(@"(?s)\bxref\s.*?\btrailer")]
    private static partial Regex CrossReference();

    [GeneratedRegex(@"startxref\s+\d+")]
    private static partial Regex StartXref();
}
