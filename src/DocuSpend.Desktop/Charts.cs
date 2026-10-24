using DocuSpend.Categorization.Model;
using DocuSpend.Presentation;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace DocuSpend.Desktop;

/// <summary>
/// Maps the statement view's plain data to LiveCharts series and axes. This is the only code that knows the chart
/// library; everything it shows is computed and formatted in DocuSpend.Presentation.
/// </summary>
internal static class Charts
{
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

    /// <summary>One series per slice, so a click on a slice tells which category it is.</summary>
    public static (ISeries[] Series, IReadOnlyDictionary<ISeries, CategoryId> Categories) Pie(IReadOnlyList<PieSlice> slices)
    {
        var categories = new Dictionary<ISeries, CategoryId>();
        var series = slices.Select(slice =>
        {
            var pie = new PieSeries<double>
            {
                Values = [(double)slice.Spending],
                Name = slice.Name,
                ToolTipLabelFormatter = _ => $"{slice.AmountText} ({slice.ShareText})",
                // A label only where there is room for it; the legend and the tooltip name every slice. A paint
                // alone switches labels on, so small slices get none.
                DataLabelsPaint = slice.Share >= 0.06m ? new SolidColorPaint(SKColors.White) : null,
                DataLabelsSize = 13,
                DataLabelsFormatter = _ => slice.ShareText,
            };
            categories[pie] = slice.Category;
            return (ISeries)pie;
        }).ToArray();
        return (series, categories);
    }

    public static ISeries[] Balance(IReadOnlyList<BalancePoint> points) =>
    [
        new StepLineSeries<DateTimePoint>
        {
            Values = points.Select(p => new DateTimePoint(p.Date.ToDateTime(TimeOnly.MinValue), (double)p.Balance)).ToArray(),
            Name = Texts.Balance,
            Fill = null,
            GeometrySize = 6,
            YToolTipLabelFormatter = point => SlovakFormat.Amount((decimal)(point.Model?.Value ?? 0)),
            XToolTipLabelFormatter = point => SlovakFormat.Date(DateOnly.FromDateTime(point.Model?.DateTime ?? default)),
        },
    ];

    public static Axis[] DateAxis() =>
    [
        new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("d. M.", SlovakFormat.Culture)) { LabelsRotation = 0 },
    ];

    public static Axis[] AmountAxis() =>
    [
        new Axis { Labeler = value => SlovakFormat.Amount((decimal)value) },
    ];
}
