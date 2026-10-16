using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DocuSpend.SampleGenerator.Rendering;

/// <summary>
/// Makes PDFsharp output byte-stable: replaces the random font subset tags and the random document ID
/// with values derived from a seed. Replacements keep their length, so cross-reference offsets stay valid.
/// </summary>
internal static partial class PdfNormalizer
{
    public static byte[] Normalize(byte[] pdf, string seed)
    {
        // Latin-1 maps every byte to one char and back, so binary content survives the round trip.
        var text = Encoding.Latin1.GetString(pdf);

        var tags = new Dictionary<string, string>();
        text = SubsetTag().Replace(text, m =>
        {
            if (!tags.TryGetValue(m.Value, out var stable))
            {
                var n = tags.Count;
                stable = $"DSAA{(char)('A' + n / 26)}{(char)('A' + n % 26)}+";
                tags[m.Value] = stable;
            }
            return stable;
        });

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed)));
        text = DocumentId().Replace(text, m =>
            $"/ID[<{hash[..m.Groups[1].Length]}><{hash[..m.Groups[2].Length]}>]");

        // XMP metadata carries random DocumentID and InstanceID UUIDs.
        var uuids = 0;
        text = XmpUuid().Replace(text, _ =>
        {
            var h = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{seed}#{uuids++}"))).ToLowerInvariant();
            return $"uuid:{h[..8]}-{h[8..12]}-{h[12..16]}-{h[16..20]}-{h[20..32]}";
        });

        return Encoding.Latin1.GetBytes(text);
    }

    [GeneratedRegex(@"uuid:[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex XmpUuid();

    [GeneratedRegex(@"\b[A-Z]{6}\+(?=Liberation)")]
    private static partial Regex SubsetTag();

    [GeneratedRegex(@"/ID\[<([0-9A-Fa-f]+)><([0-9A-Fa-f]+)>\]")]
    private static partial Regex DocumentId();
}
