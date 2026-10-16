using PdfSharp.Fonts;

namespace DocuSpend.SampleGenerator.Rendering;

/// <summary>Serves the bundled Liberation fonts (SIL OFL 1.1, see Fonts/LICENSE-Liberation.txt) so output is the same on every machine.</summary>
public sealed class EmbeddedFontResolver : IFontResolver
{
    public const string Mono = "Liberation Mono";
    public const string Sans = "Liberation Sans";

    private static readonly Lock InstallLock = new();

    public static void Install()
    {
        lock (InstallLock)
        {
            if (GlobalFontSettings.FontResolver is not EmbeddedFontResolver)
                GlobalFontSettings.FontResolver = new EmbeddedFontResolver();
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) => familyName switch
    {
        Mono => new FontResolverInfo(bold ? "LiberationMono-Bold" : "LiberationMono-Regular"),
        Sans => new FontResolverInfo("LiberationSans-Regular"),
        _ => null,
    };

    public byte[]? GetFont(string faceName)
    {
        using var stream = typeof(EmbeddedFontResolver).Assembly
            .GetManifestResourceStream($"DocuSpend.SampleGenerator.Fonts.{faceName}.ttf");
        if (stream is null)
            return null;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
