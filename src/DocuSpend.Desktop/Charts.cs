using DocuSpend.Categorization.Model;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace DocuSpend.Desktop;

/// <summary>
/// Maps the statement view's plain data to LiveCharts series and axes, in the colours of the current look. This is
/// the only code that knows the chart library; everything it shows is computed and formatted in
/// DocuSpend.Presentation, and every colour comes from the palette.
/// </summary>
internal static class Charts
{
    /// <summary>The ring's area is a fixed square, so the ring never shrinks for a long legend.</summary>
    public const double RingSize = 260;

    /// <summary>The ring's hole: room for the spending total in its centre.</summary>
    private const double RingHole = 78;

    /// <summary>
    /// Chart texts are drawn by SkiaSharp, not by Avalonia, with a typeface SkiaSharp picks unless told otherwise.
    /// Segoe UI, Windows' own font, has every Slovak letter.
    /// </summary>
    /// <param name="animate">False for screenshots, so a chart is complete the moment it is drawn.</param>
    public static void Configure(bool animate = true) =>
        LiveCharts.Configure(config =>
        {
            config.HasTextSettings(new TextSettings { DefaultTypeface = SKTypeface.FromFamilyName("Segoe UI") });
            if (!animate)
                config.WithAnimationsSpeed(TimeSpan.Zero);
        });

    public static SKColor Color(string hex) => SKColor.Parse(hex);

    /// <summary>
    /// The spending ring: one series per slice, so a click on a slice tells which category it is. Slices keep their
    /// category's colour and are separated by a thin line in the card's colour. The legend is the window's own.
    /// </summary>
    public static (ISeries[] Series, IReadOnlyDictionary<ISeries, CategoryId> Categories) Ring(IReadOnlyList<PieSlice> slices, Palette palette)
    {
        var categories = new Dictionary<ISeries, CategoryId>();
        var series = slices.Select(slice =>
        {
            var pie = new PieSeries<double>
            {
                Values = [(double)slice.Spending],
                Name = slice.Name,
                Fill = new SolidColorPaint(Color(slice.Color)),
                Stroke = new SolidColorPaint(Color(palette.Card), 2),
                InnerRadius = RingHole,
                HoverPushout = 6,
                IsVisibleAtLegend = false,
                ToolTipLabelFormatter = _ => $"{slice.AmountText} ({slice.ShareText})",
            };
            categories[pie] = slice.Category;
            return (ISeries)pie;
        }).ToArray();
        return (series, categories);
    }

    /// <summary>
    /// The balance: a stepped line, because the balance changes on booking dates and holds in between, in the accent
    /// colour with a soft fill fading downwards. No markers; hovering shows the date and the balance.
    /// </summary>
    public static ISeries[] Balance(IReadOnlyList<BalancePoint> points, Palette palette)
    {
        var accent = Color(palette.Accent);
        return
        [
            new StepLineSeries<DateTimePoint>
            {
                Values = points.Select(p => new DateTimePoint(p.Date.ToDateTime(TimeOnly.MinValue), (double)p.Balance)).ToArray(),
                Name = Texts.Balance,
                Stroke = new SolidColorPaint(accent, 3),
                Fill = new LinearGradientPaint([accent.WithAlpha(0x55), accent.WithAlpha(0x00)], new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                GeometrySize = 0,
                GeometryFill = null,
                GeometryStroke = null,
                YToolTipLabelFormatter = point => SlovakFormat.Amount((decimal)(point.Model?.Value ?? 0)),
                XToolTipLabelFormatter = point => SlovakFormat.Date(DateOnly.FromDateTime(point.Model?.DateTime ?? default)),
            },
        ];
    }

    /// <summary>Days along the bottom: muted labels, no grid lines.</summary>
    public static Axis[] DateAxis(Palette palette) =>
    [
        new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("d. M.", SlovakFormat.Culture))
        {
            LabelsRotation = 0,
            LabelsPaint = new SolidColorPaint(Color(palette.Muted)),
            SeparatorsPaint = null,
            TextSize = 12,
        },
    ];

    /// <summary>Amounts on the side: muted labels and faint grid lines.</summary>
    public static Axis[] AmountAxis(Palette palette) =>
    [
        new Axis
        {
            Labeler = value => SlovakFormat.Amount((decimal)value),
            LabelsPaint = new SolidColorPaint(Color(palette.Muted)),
            SeparatorsPaint = new SolidColorPaint(Color(palette.Grid), 1),
            TextSize = 12,
        },
    ];

    /// <summary>The hover card: the look's card with its text colour.</summary>
    public static (SolidColorPaint Background, SolidColorPaint Text) Tooltip(Palette palette) =>
        (new SolidColorPaint(Color(palette.Card)), new SolidColorPaint(Color(palette.Text)));
}
