namespace DocuSpend.Presentation;

/// <summary>A position, in the window's units.</summary>
public readonly record struct HoverPoint(double X, double Y);

/// <summary>A card's size.</summary>
public readonly record struct HoverSize(double Width, double Height);

/// <summary>The area a card has to stay inside: the window's content.</summary>
public readonly record struct HoverArea(double Left, double Top, double Right, double Bottom);

/// <summary>Where a hover card goes: beside what it is about, and whole inside the window.</summary>
public static class HoverCardPlacement
{
    /// <summary>
    /// The card's top-left corner. The card goes right of and below the anchor, with a gap; where that would cross the
    /// area's right or bottom edge it goes left or above instead. Then it is moved inside the area. A card larger
    /// than the area is pinned to the area's top-left corner.
    /// </summary>
    public static HoverPoint Place(HoverPoint anchor, HoverSize card, HoverArea area, double gap)
    {
        var x = anchor.X + gap + card.Width > area.Right ? anchor.X - gap - card.Width : anchor.X + gap;
        var y = anchor.Y + gap + card.Height > area.Bottom ? anchor.Y - gap - card.Height : anchor.Y + gap;
        return new HoverPoint(
            Math.Max(area.Left, Math.Min(x, area.Right - card.Width)),
            Math.Max(area.Top, Math.Min(y, area.Bottom - card.Height)));
    }
}
