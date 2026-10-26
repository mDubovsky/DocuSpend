namespace DocuSpend.Presentation.Look;

/// <summary>
/// The look's colours, as "#RRGGBB", in one place for the window and the charts alike (spec: one look, defined once).
/// Contrast between the pairs the spec names is tested, so a colour can't be changed into an unreadable one.
/// </summary>
public sealed record Palette(
    string Background,
    string Card,
    string CardEdge,
    string Hover,
    string Accent,
    string Primary,
    string OnPrimary,
    string AccentTint,
    string Text,
    string Muted,
    string Positive,
    string PositiveTint,
    string Error,
    string ErrorTint,
    string Grid,
    string SwitchHover)
{
    /// <summary>White cards on a pale grey-blue, with a blue-violet accent.</summary>
    public static Palette Light { get; } = new(
        Background: "#F2F4FA",
        Card: "#FFFFFF",
        CardEdge: "#E2E5F0",
        Hover: "#F3F4FA",
        Accent: "#4F5BD5",
        Primary: "#4F5BD5",
        OnPrimary: "#FFFFFF",
        AccentTint: "#E4E7FB",
        Text: "#1A1D2E",
        Muted: "#5B6180",
        Positive: "#17784D",
        PositiveTint: "#E6F5EE",
        Error: "#C02A37",
        ErrorTint: "#FDECEE",
        Grid: "#E6E8F0",
        SwitchHover: "#DCDFEA");

    /// <summary>
    /// Deep navy, slightly lighter cards, the same accent lightened to stand out on dark. Buttons use a deeper violet
    /// than the accent, so their text can be white: dark text on the light accent was hard to read.
    /// </summary>
    public static Palette Dark { get; } = new(
        Background: "#11142A",
        Card: "#1B1F38",
        CardEdge: "#2A2F52",
        Hover: "#232847",
        Accent: "#7C89FF",
        Primary: "#5560E0",
        OnPrimary: "#FFFFFF",
        AccentTint: "#2C3270",
        Text: "#ECEEF8",
        Muted: "#A3A9CC",
        Positive: "#4FCB8F",
        PositiveTint: "#173327",
        Error: "#FF8A93",
        ErrorTint: "#3B1F2E",
        Grid: "#2A2F4D",
        SwitchHover: "#3A3F63");

    /// <summary>Every colour by its name, as the window's resources are named.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> All() =>
    [
        new(nameof(Background), Background), new(nameof(Card), Card), new(nameof(CardEdge), CardEdge),
        new(nameof(Hover), Hover), new(nameof(Accent), Accent), new(nameof(Primary), Primary), new(nameof(OnPrimary), OnPrimary),
        new(nameof(AccentTint), AccentTint), new(nameof(Text), Text), new(nameof(Muted), Muted),
        new(nameof(Positive), Positive), new(nameof(PositiveTint), PositiveTint), new(nameof(Error), Error),
        new(nameof(ErrorTint), ErrorTint), new(nameof(Grid), Grid), new(nameof(SwitchHover), SwitchHover),
    ];

    public string Get(string name) => All().Single(c => c.Key == name).Value;
}
