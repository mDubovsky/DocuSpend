using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using DocuSpend.Presentation;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;

namespace DocuSpend.Desktop;

/// <summary>
/// The charts' hover cards. The chart library says what is under the pointer; the cards themselves are ordinary
/// controls in a layer over the whole window, so no chart can cut one, and they are placed so that they stay inside
/// the window (work/020-chart-hover-cards/plan.md).
/// </summary>
public partial class MainWindow
{
    private PieSlice? _hoveredSlice;
    private Point _ringPointer;

    private void WireHoverCards()
    {
        SpendingPie.HoveredPointsChanged += OnRingHover;
        SpendingPie.PointerMoved += (_, e) =>
        {
            _ringPointer = e.GetPosition(HoverLayer);
            if (_hoveredSlice is { } slice)
                ShowRingCard(slice, _ringPointer);
        };
        SpendingPie.PointerExited += (_, _) => HideHoverCards();
        BalanceLine.PointerMoved += OnBalanceMoved;
        BalanceLine.PointerExited += (_, _) => HideHoverCards();
        // A card would stay where the pointer was while the content moves under it.
        AddHandler(ScrollViewer.ScrollChangedEvent, (_, _) => HideHoverCards());
    }

    private void OnRingHover(IChartView chart, IEnumerable<ChartPoint>? hovered, IEnumerable<ChartPoint>? left)
    {
        var series = hovered?.FirstOrDefault()?.Context.Series;
        var slice = series is not null && _sliceCategories.TryGetValue(series, out var category)
            ? _model.Statement.Slices.FirstOrDefault(s => s.Category == category)
            : null;
        if (slice is null)
            HideHoverCards();
        else
            ShowRingCard(slice, _ringPointer);
    }

    private void OnBalanceMoved(object? sender, PointerEventArgs e)
    {
        var points = _model.Statement.BalancePoints;
        var chart = BalanceLine.CoreChart;
        var at = e.GetPosition(BalanceLine);
        var (left, top) = (chart.DrawMarginLocation.X, chart.DrawMarginLocation.Y);
        if (points.Count == 0 || at.X < left || at.X > left + chart.DrawMarginSize.Width || at.Y < top || at.Y > top + chart.DrawMarginSize.Height)
        {
            HideHoverCards();
            return;
        }

        var position = BalanceLine.ScalePixelsToData(new LvcPointD(at.X, at.Y));
        var date = new DateTime((long)Math.Clamp(position.X, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks));
        var day = BalanceDays.At(date, points[0].Date, points[^1].Date);
        var point = points[day.DayNumber - points[0].Date.DayNumber];
        // The dot sits at the start of the day's stretch of the line, at the day's balance.
        var onLine = BalanceLine.ScaleDataToPixels(new LvcPointD(day.ToDateTime(TimeOnly.MinValue).Ticks, (double)point.Balance));
        ShowBalanceDay(point, BalanceLine.TranslatePoint(new Point(onLine.X, onLine.Y), HoverLayer) ?? default);
    }

    /// <summary>Shows the ring's card for a slice beside <paramref name="anchor"/>, in the layer's coordinates.</summary>
    internal void ShowRingCard(PieSlice slice, Point anchor)
    {
        _hoveredSlice = slice;
        BalanceCard.IsVisible = HoverDot.IsVisible = false;
        RingCardName.Text = slice.Name;
        RingCardAmount.Text = $"{slice.AmountText} ({slice.ShareText})";
        PlaceCard(RingCard, anchor, Gap);
    }

    /// <summary>Shows the balance card for a day, and the dot at <paramref name="dot"/> (in the layer's coordinates) on the line.</summary>
    internal void ShowBalanceDay(BalancePoint day, Point dot)
    {
        _hoveredSlice = null;
        RingCard.IsVisible = false;
        BalanceCardDate.Text = day.DateText;
        BalanceCardBalance.Text = $"{Presentation.Texts.Balance} {day.BalanceText}";
        var size = HoverDot.Width;
        Canvas.SetLeft(HoverDot, dot.X - size / 2);
        Canvas.SetTop(HoverDot, dot.Y - size / 2);
        HoverDot.IsVisible = true;
        PlaceCard(BalanceCard, dot, Gap + size / 2);
    }

    internal void HideHoverCards()
    {
        _hoveredSlice = null;
        RingCard.IsVisible = BalanceCard.IsVisible = HoverDot.IsVisible = false;
    }

    private double Gap => Space("Look.Space.HoverGap", 14);

    private double Space(string key, double fallback) =>
        this.TryFindResource(key, ActualThemeVariant, out var value) && value is double size ? size : fallback;

    /// <summary>Puts a card beside the anchor, inside the window; a name too long for the window's width is shortened.</summary>
    private void PlaceCard(Border card, Point anchor, double gap)
    {
        var edge = Space("Look.Space.HoverEdge", 8);
        var area = new HoverArea(edge, edge, HoverLayer.Bounds.Width - edge, HoverLayer.Bounds.Height - edge);
        card.MaxWidth = Math.Max(0, area.Right - area.Left);
        card.IsVisible = true;
        card.Measure(Size.Infinity);
        var size = card.DesiredSize;
        var spot = HoverCardPlacement.Place(new HoverPoint(anchor.X, anchor.Y), new HoverSize(size.Width, size.Height), area, gap);
        Canvas.SetLeft(card, spot.X);
        Canvas.SetTop(card, spot.Y);
    }
}
