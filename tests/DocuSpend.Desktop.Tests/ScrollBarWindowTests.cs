using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// Scroll bars in the real window (work/019-look-switch-scroll-bars/spec.md): one look for all of them, always shown
/// when a list has more than fits, in a strip of their own, and scrolling as before.
/// </summary>
public sealed class ScrollBarWindowTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("docuspend-scroll-bars-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    /// <summary>The busy month with its largest category selected, so the transaction list overflows, and the built-in rules shown.</summary>
    private (MainWindow Window, MainViewModel Model) Start(LookChoice look = LookChoice.Light)
    {
        var model = new MainViewModel(new RuleStore(Path.Join(_folder, "rules.json")));
        model.SelectedLook = model.Looks.Single(l => l.Choice == look);
        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample("02-busy-month")));
        model.Statement.SelectedRow = model.Statement.Categories.MaxBy(row =>
        {
            model.Statement.SelectedRow = row;
            return model.Statement.Transactions.Count;
        })!;
        model.SelectedRuleView = model.RuleViews.Single(v => v.View == RuleView.BuiltIn);
        var window = new MainWindow(model);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, model);
    }

    private static void Finish(Window window)
    {
        window.Close();
        Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
    }

    private static string Hex(IBrush? brush) =>
        brush is ISolidColorBrush b ? $"#{b.Color.R:X2}{b.Color.G:X2}{b.Color.B:X2}" : "none";

    private static ListBox Transactions(MainWindow window, MainViewModel model) =>
        window.GetVisualDescendants().OfType<ListBox>().Single(l => ReferenceEquals(l.ItemsSource, model.Statement.Transactions));

    private static ScrollViewer ScrollerOf(Control list) =>
        list as ScrollViewer ?? list.GetVisualDescendants().OfType<ScrollViewer>().First();

    private static ScrollBar VerticalBar(ScrollViewer scroller) =>
        scroller.GetVisualDescendants().OfType<ScrollBar>().First(b => b.Orientation == Avalonia.Layout.Orientation.Vertical
            && ReferenceEquals(b.GetVisualAncestors().OfType<ScrollViewer>().First(), scroller));

    private static Thumb ThumbOf(ScrollBar bar) => bar.GetVisualDescendants().OfType<Thumb>().Single();

    private static double Resource(Window window, string key) => (double)window.FindResource(key)!;

    /// <summary>Brings the list into view, as a user would scroll the window to it, and returns its scroll bar.</summary>
    private static (ScrollViewer Scroller, ScrollBar Bar) InView(Control list)
    {
        list.BringIntoView();
        Dispatcher.UIThread.RunJobs();
        var scroller = ScrollerOf(list);
        if (list is not ListBox { Classes: var c } || !c.Contains("segmented"))
            Assert.True(scroller.Extent.Height > scroller.Viewport.Height, "The list must have more than fits for this test.");
        return (scroller, VerticalBar(scroller));
    }

    private static Point Centre(Window window, Control control) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    private static IEnumerable<ScrollBar> EveryBar(Visual root) => root.GetVisualDescendants().OfType<ScrollBar>();

    private static void AssertTheLooksBar(Window window, ScrollBar bar, Palette palette)
    {
        bar.ApplyTemplate();
        var buttons = bar.GetVisualDescendants().OfType<RepeatButton>().Select(b => b.Name).ToList();
        Assert.DoesNotContain("PART_LineUpButton", buttons);
        Assert.DoesNotContain("PART_LineDownButton", buttons);
        var thumb = ThumbOf(bar);
        var thickness = bar.Orientation == Avalonia.Layout.Orientation.Vertical ? thumb.Width : thumb.Height;
        Assert.Equal(Resource(window, "Look.Size.ScrollThumb"), thickness);
        Assert.Equal(palette.ScrollThumb, Hex(thumb.Background));
        Assert.False(bar.GetVisualAncestors().OfType<ScrollViewer>().First().AllowAutoHide);
    }

    [Fact]
    public void Every_scroll_bar_in_the_window_and_in_a_choice_boxs_list_has_the_looks_handle_and_no_arrows() => Headless.Run(() =>
    {
        var (window, model) = Start();

        // Each screen's bars: the legend's and the two lists' on the statement screen, the rule lists' on the other.
        var statementBars = EveryBar(window).ToList();
        Assert.True(statementBars.Count >= 3, $"Expected the legend's and the lists' bars; found {statementBars.Count}.");
        Assert.All(statementBars, bar => AssertTheLooksBar(window, bar, Palette.Light));
        Headless.ShowRules(model);
        var ruleBars = EveryBar(window).Except(statementBars).ToList();
        Assert.True(ruleBars.Count >= 1, $"Expected the rule lists' bars; found {ruleBars.Count}.");
        Assert.All(ruleBars, bar => AssertTheLooksBar(window, bar, Palette.Light));

        var categories = window.GetVisualDescendants().OfType<ComboBox>().Single(c => ReferenceEquals(c.ItemsSource, model.RuleForm.Categories));
        categories.BringIntoView();
        // The test platform's font is small, so the list is made short enough to scroll, as it does on a real screen.
        categories.MaxDropDownHeight = 60;
        categories.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();
        var popup = categories.GetVisualDescendants().OfType<Popup>().Single();
        var popupBars = EveryBar(popup.Child!).ToList();
        Assert.NotEmpty(popupBars);
        Assert.All(popupBars, bar => AssertTheLooksBar(window, bar, Palette.Light));
        Assert.Contains(popupBars, b => b.IsVisible);
        categories.IsDropDownOpen = false;
        Finish(window);
    });

    [Fact]
    public void A_list_with_more_than_fits_shows_its_bar_without_the_pointer_and_one_that_fits_shows_none() => Headless.Run(() =>
    {
        var (window, model) = Start();

        var (_, overflowing) = InView(Transactions(window, model));
        var (_, fitting) = InView(window.LookSwitch);
        Assert.True(overflowing.IsVisible);
        Assert.False(overflowing.IsPointerOver);

        Headless.ShowRules(model);
        var (_, builtInRules) = InView(window.FindControl<ScrollViewer>("BuiltInRules")!);
        Assert.True(builtInRules.IsVisible);
        Assert.False(fitting.IsVisible);
        Finish(window);
    });

    [Fact]
    public void A_list_that_scrolls_leaves_its_bar_a_strip_of_its_own() => Headless.Run(() =>
    {
        var (window, model) = Start();
        var (scroller, bar) = InView(Transactions(window, model));

        var content = scroller.GetVisualDescendants().OfType<ScrollContentPresenter>().First();
        var barLeft = bar.TranslatePoint(new Point(0, 0), scroller)!.Value.X;

        Assert.Equal(Resource(window, "Look.Size.ScrollBar"), bar.Bounds.Width);
        Assert.Equal(scroller.Bounds.Width - bar.Bounds.Width, content.Bounds.Width, tolerance: 0.5);
        Assert.All(scroller.GetVisualDescendants().OfType<ListBoxItem>(), row =>
            Assert.True(row.TranslatePoint(new Point(row.Bounds.Width, 0), scroller)!.Value.X <= barLeft + 0.5,
                "A row reaches under the scroll bar."));
        Finish(window);
    });

    [Fact]
    public void The_mouse_wheel_scrolls_a_list() => Headless.Run(() =>
    {
        var (window, model) = Start();
        var (scroller, _) = InView(Transactions(window, model));

        window.MouseWheel(Centre(window, scroller), new Vector(0, -1));
        Dispatcher.UIThread.RunJobs();

        Assert.True(scroller.Offset.Y > 0);
        Finish(window);
    });

    [Fact]
    public void Dragging_the_handle_scrolls_a_list() => Headless.Run(() =>
    {
        var (window, model) = Start();
        var (scroller, bar) = InView(Transactions(window, model));
        var from = Centre(window, ThumbOf(bar));

        window.MouseDown(from, MouseButton.Left);
        window.MouseMove(from + new Point(0, 40));
        window.MouseUp(from + new Point(0, 40), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(scroller.Offset.Y > 0);
        Finish(window);
    });

    [Fact]
    public void Pressing_the_track_below_the_handle_scrolls_a_list() => Headless.Run(() =>
    {
        var (window, model) = Start();
        var (scroller, bar) = InView(Transactions(window, model));
        var nearTheEnd = bar.TranslatePoint(new Point(bar.Bounds.Width / 2, bar.Bounds.Height - 4), window)!.Value;

        window.MouseDown(nearTheEnd, MouseButton.Left);
        window.MouseUp(nearTheEnd, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(scroller.Offset.Y > 0);
        Finish(window);
    });

    [Theory]
    [InlineData(LookChoice.Light)]
    [InlineData(LookChoice.Dark)]
    public void The_handle_is_stronger_under_the_pointer(LookChoice look) => Headless.Run(() =>
    {
        var (window, model) = Start(look);
        var palette = look == LookChoice.Dark ? Palette.Dark : Palette.Light;
        var (_, bar) = InView(Transactions(window, model));
        var thumb = ThumbOf(bar);
        Assert.Equal(palette.ScrollThumb, Hex(thumb.Background));

        window.MouseMove(Centre(window, thumb));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(palette.Muted, Hex(thumb.Background));
        Finish(window);
    });

    [Fact]
    public void Switching_the_look_recolours_a_scrolled_list_and_keeps_its_place() => Headless.Run(() =>
    {
        var (window, model) = Start(LookChoice.Light);
        var (scroller, bar) = InView(Transactions(window, model));
        scroller.Offset = new Vector(0, 60);
        Dispatcher.UIThread.RunJobs();

        model.SelectedLook = model.Looks.Single(l => l.Choice == LookChoice.Dark);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Palette.Dark.ScrollThumb, Hex(ThumbOf(bar).Background));
        Assert.Equal(60, scroller.Offset.Y, tolerance: 0.5);
        Finish(window);
    });
}
