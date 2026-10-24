using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using DocuSpend.Presentation.Look;

namespace DocuSpend.Desktop.Look;

/// <summary>
/// Turns the palette into the application's resources: a Look.&lt;name&gt; brush per colour for the light and the dark
/// variant, and the Fluent theme's own palettes, so the toolkit's controls (choice boxes, text fields, scroll bars)
/// match the look too.
/// </summary>
internal static class LookResources
{
    public static void Apply(Application app)
    {
        var resources = new ResourceDictionary();
        resources.ThemeDictionaries[ThemeVariant.Light] = Brushes(Palette.Light);
        resources.ThemeDictionaries[ThemeVariant.Dark] = Brushes(Palette.Dark);
        app.Resources.MergedDictionaries.Add(resources);

        var fluent = app.Styles.OfType<FluentTheme>().Single();
        fluent.Palettes[ThemeVariant.Light] = FluentPalette(Palette.Light);
        fluent.Palettes[ThemeVariant.Dark] = FluentPalette(Palette.Dark);
    }

    /// <summary>The palette for the given variant: what the charts draw with.</summary>
    public static Palette For(ThemeVariant variant) => variant == ThemeVariant.Dark ? Palette.Dark : Palette.Light;

    private static ResourceDictionary Brushes(Palette palette)
    {
        var dictionary = new ResourceDictionary();
        foreach (var (name, hex) in palette.All())
        {
            var color = Color.Parse(hex);
            dictionary["Look." + name] = new SolidColorBrush(color);
            dictionary["Look." + name + ".Color"] = color;
        }
        return dictionary;
    }

    private static ColorPaletteResources FluentPalette(Palette palette) => new()
    {
        Accent = Color.Parse(palette.Accent),
        RegionColor = Color.Parse(palette.Card),
        BaseHigh = Color.Parse(palette.Text),
        BaseMedium = Color.Parse(palette.Muted),
        ErrorText = Color.Parse(palette.Error),
    };
}
