namespace DocuSpend.Presentation.Tests;

/// <summary>Where a hover card goes: beside its anchor, and always whole inside the window (spec: both cards).</summary>
public class HoverCardPlacementTests
{
    private static readonly HoverArea Area = new(0, 0, 1000, 800);
    private static readonly HoverSize Card = new(200, 60);
    private const double Gap = 12;

    private static HoverPoint Place(double x, double y, HoverSize? card = null, HoverArea? area = null) =>
        HoverCardPlacement.Place(new HoverPoint(x, y), card ?? Card, area ?? Area, Gap);

    [Fact]
    public void By_default_the_card_goes_right_of_and_below_the_anchor()
    {
        var spot = Place(300, 200);

        Assert.Equal(300 + Gap, spot.X);
        Assert.Equal(200 + Gap, spot.Y);
    }

    [Fact]
    public void Near_the_right_edge_the_card_goes_left_of_the_anchor()
    {
        var spot = Place(950, 200);

        Assert.Equal(950 - Gap - Card.Width, spot.X);
        Assert.Equal(200 + Gap, spot.Y);
    }

    [Fact]
    public void Near_the_bottom_the_card_goes_above_the_anchor()
    {
        var spot = Place(300, 780);

        Assert.Equal(300 + Gap, spot.X);
        Assert.Equal(780 - Gap - Card.Height, spot.Y);
    }

    [Fact]
    public void In_the_bottom_right_corner_the_card_goes_above_and_left()
    {
        var spot = Place(990, 790);

        Assert.Equal(990 - Gap - Card.Width, spot.X);
        Assert.Equal(790 - Gap - Card.Height, spot.Y);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1000, 0)]
    [InlineData(0, 800)]
    [InlineData(1000, 800)]
    [InlineData(500, 400)]
    [InlineData(5, 790)]
    [InlineData(-50, 900)]
    public void The_card_is_never_outside_the_area_when_it_fits(double x, double y)
    {
        var spot = Place(x, y);

        Assert.InRange(spot.X, Area.Left, Area.Right - Card.Width);
        Assert.InRange(spot.Y, Area.Top, Area.Bottom - Card.Height);
    }

    [Fact]
    public void A_card_larger_than_the_area_is_pinned_to_the_areas_top_left()
    {
        var spot = Place(100, 100, new HoverSize(600, 500), new HoverArea(10, 20, 400, 300));

        Assert.Equal(new HoverPoint(10, 20), spot);
    }

    [Fact]
    public void An_area_that_does_not_start_at_the_origin_is_respected()
    {
        var area = new HoverArea(50, 40, 500, 400);

        var spot = Place(55, 45, area: area);

        Assert.InRange(spot.X, 50, 500 - Card.Width);
        Assert.InRange(spot.Y, 40, 400 - Card.Height);
    }
}
