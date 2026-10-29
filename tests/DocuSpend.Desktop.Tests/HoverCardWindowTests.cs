using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;
using LiveChartsCore.Measure;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// The charts' hover cards (work/020-chart-hover-cards/spec.md). The charts aren't laid out on the headless platform
/// without Skia, so nothing can be hovered here: the tests give the window what a hover would, a slice or a day and
/// a position. That the real hovers find them is for the screenshots.
/// </summary>
public sealed class HoverCardWindowTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private (MainWindow Window, MainViewModel Model) Open(string sample = "02-busy-month", double width = 1280, double height = 940)
    {
        var model = new MainViewModel(new RuleStore(Path.Combine(_folder, "rules.json")));
        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample(sample)));
        var window = new MainWindow(model) { Width = width, Height = height };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, model);
    }

    private static void Finish(Window window)
    {
        window.Close();
        Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
    }

    private static Rect Bounds(Control card) =>
        new(Canvas.GetLeft(card), Canvas.GetTop(card), card.DesiredSize.Width, card.DesiredSize.Height);

    private static PieSlice Slice(string name) => new(new CategoryId("housing"), name, 48.90m, 0.031m, "#336699");

    private static string Hex(IBrush? brush)
    {
        var color = ((ISolidColorBrush)brush!).Color;
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    [Fact]
    public void The_librarys_own_cards_are_off_so_only_ours_show() => Headless.Run(() =>
    {
        var (window, _) = Open();

        Assert.Equal(TooltipPosition.Hidden, window.SpendingPie.TooltipPosition);
        Assert.Equal(TooltipPosition.Hidden, window.BalanceLine.TooltipPosition);
        Finish(window);
    });

    [Fact]
    public void The_rings_card_shows_the_name_then_the_amount_and_the_share() => Headless.Run(() =>
    {
        var (window, model) = Open();
        var slice = model.Statement.Slices.Single(s => s.Name.StartsWith("Elektronika", StringComparison.Ordinal));

        window.ShowRingCard(slice, new Point(300, 300));

        Assert.True(window.RingCard.IsVisible);
        Assert.Equal("Elektronika, komunikácia a médiá", window.RingCardName.Text);
        Assert.Equal($"{slice.AmountText} ({slice.ShareText})", window.RingCardAmount.Text);
        Assert.False(window.BalanceCard.IsVisible);
        Finish(window);
    });

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1280, 0)]
    [InlineData(0, 940)]
    [InlineData(1280, 940)]
    [InlineData(640, 440)]
    public void A_card_stays_whole_inside_the_window_wherever_the_pointer_is(double x, double y) => Headless.Run(() =>
    {
        var (window, model) = Open();
        var layer = window.HoverLayer.Bounds;

        window.ShowRingCard(model.Statement.Slices[0], new Point(x, y));
        var ring = Bounds(window.RingCard);
        window.ShowBalanceDay(model.Statement.BalancePoints[3], new Point(x, y));
        var balance = Bounds(window.BalanceCard);

        foreach (var card in new[] { ring, balance })
        {
            Assert.True(card.Left >= 0 && card.Top >= 0, $"{card} starts outside the window");
            Assert.True(card.Right <= layer.Width && card.Bottom <= layer.Height, $"{card} ends outside the window");
        }
        Finish(window);
    });

    [Fact]
    public void A_name_too_long_for_the_window_is_shortened_and_the_amount_and_share_are_not() => Headless.Run(() =>
    {
        var (window, _) = Open(width: 720, height: 600);
        var slice = Slice(string.Join(" ", Enumerable.Repeat("Veľmi dlhý názov kategórie", 12)));

        window.ShowRingCard(slice, new Point(100, 100));

        Assert.True(window.RingCard.DesiredSize.Width <= window.HoverLayer.Bounds.Width);
        Assert.Equal(TextTrimming.CharacterEllipsis, window.RingCardName.TextTrimming);
        Assert.Equal(slice.Name, window.RingCardName.Text);
        Assert.Equal("48,90 € (3,1 %)", window.RingCardAmount.Text);
        Assert.Equal(TextTrimming.None, window.RingCardAmount.TextTrimming);
        Finish(window);
    });

    [Fact]
    public void At_the_size_DocuSpend_opens_at_no_built_in_category_name_is_shortened() => Headless.Run(() =>
    {
        var (window, _) = Open();

        foreach (var category in CategorizationConfig.Default.Catalog.Categories)
        {
            window.ShowRingCard(Slice(category.Name), new Point(1200, 100));
            Assert.True(window.RingCard.DesiredSize.Width < window.RingCard.MaxWidth, $"{category.Name} is shortened");
        }
        Finish(window);
    });

    [Fact]
    public void The_balance_card_shows_the_date_then_the_balance_and_the_dot_marks_the_day() => Headless.Run(() =>
    {
        var (window, model) = Open();
        var day = model.Statement.BalancePoints[13];

        window.ShowBalanceDay(day, new Point(400, 500));

        Assert.True(window.BalanceCard.IsVisible);
        Assert.Equal(day.DateText, window.BalanceCardDate.Text);
        Assert.Equal($"Zostatok {day.BalanceText}", window.BalanceCardBalance.Text);
        Assert.True(window.HoverDot.IsVisible);
        Assert.Equal(400, Canvas.GetLeft(window.HoverDot) + window.HoverDot.Width / 2, 3);
        Assert.Equal(500, Canvas.GetTop(window.HoverDot) + window.HoverDot.Height / 2, 3);
        Assert.False(window.RingCard.IsVisible);
        Finish(window);
    });

    [Fact]
    public void Hiding_removes_both_cards_and_the_dot() => Headless.Run(() =>
    {
        var (window, model) = Open();
        window.ShowBalanceDay(model.Statement.BalancePoints[0], new Point(400, 500));

        window.HideHoverCards();

        Assert.False(window.RingCard.IsVisible);
        Assert.False(window.BalanceCard.IsVisible);
        Assert.False(window.HoverDot.IsVisible);
        Finish(window);
    });

    [Fact]
    public void Opening_another_statement_removes_a_shown_card() => Headless.Run(() =>
    {
        var (window, model) = Open();
        window.ShowBalanceDay(model.Statement.BalancePoints[0], new Point(400, 500));

        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample("01-typical-month")));
        Dispatcher.UIThread.RunJobs();

        Assert.False(window.BalanceCard.IsVisible);
        Assert.False(window.HoverDot.IsVisible);
        Finish(window);
    });

    [Fact]
    public void Switching_the_look_recolours_a_shown_card_and_the_dot() => Headless.Run(() =>
    {
        var (window, model) = Open();
        model.SelectedLook = model.Looks.Single(l => l.Choice == LookChoice.Light);
        Dispatcher.UIThread.RunJobs();
        window.ShowBalanceDay(model.Statement.BalancePoints[0], new Point(400, 500));
        Assert.Equal(Palette.Light.Card, Hex(window.BalanceCard.Background));
        Assert.Equal(Palette.Light.Accent, Hex(window.HoverDot.Fill));

        model.SelectedLook = model.Looks.Single(l => l.Choice == LookChoice.Dark);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Palette.Dark.Card, Hex(window.BalanceCard.Background));
        Assert.Equal(Palette.Dark.Accent, Hex(window.HoverDot.Fill));
        Finish(window);
    });
}
