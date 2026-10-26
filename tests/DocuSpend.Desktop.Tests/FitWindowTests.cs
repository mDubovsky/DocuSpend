using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DocuSpend.Categorization.Model;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// The statement screen fills the window without scrolling at the size DocuSpend opens at, and the lists scroll inside
/// their cards (work/021-rules-screen/spec.md, "The statement screen"). The window tests draw with the platform's own
/// small font, so these measure the arrangement in rows of that font; with Windows' real fonts the fit is checked in
/// the screenshots and by hand.
/// </summary>
public sealed class FitWindowTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("docuspend-fit-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private const int WantedRows = 5;

    private (MainWindow Window, MainViewModel Model) Open(string sample, double width = 1280, double height = 880,
        LookChoice look = LookChoice.Light, bool longestCategory = false)
    {
        var model = new MainViewModel(new RuleStore(Path.Join(_folder, "rules.json")));
        model.SelectedLook = model.Looks.Single(l => l.Choice == look);
        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample(sample)));
        if (longestCategory)
            model.Statement.SelectedRow = model.Statement.Categories.MaxBy(row =>
            {
                model.Statement.SelectedRow = row;
                return model.Statement.Transactions.Count;
            })!;
        var window = new MainWindow(model) { Width = width, Height = height };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
        return (window, model);
    }

    private static void Finish(Window window)
    {
        window.Close();
        Application.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;
    }

    private static ListBox ListOf(MainWindow window, object items) =>
        window.GetVisualDescendants().OfType<ListBox>().Single(l => ReferenceEquals(l.ItemsSource, items));

    /// <summary>How far apart two rows of the list are: the row and the margin between rows.</summary>
    private static double RowPitch(ListBox list)
    {
        var rows = list.GetVisualDescendants().OfType<ListBoxItem>().Take(2).ToList();
        Assert.Equal(2, rows.Count);
        return rows[1].TranslatePoint(default, list)!.Value.Y - rows[0].TranslatePoint(default, list)!.Value.Y;
    }

    private static double Overflow(MainWindow window) => window.ContentScroller.Extent.Height - window.ContentScroller.Viewport.Height;

    public static TheoryData<string, LookChoice> Samples
    {
        get
        {
            var data = new TheoryData<string, LookChoice>();
            foreach (var look in new[] { LookChoice.Light, LookChoice.Dark })
                foreach (var sample in new[] { "01-typical-month", "02-busy-month", "03-awkward-month", "04-empty-month", "05-single-transaction-month" })
                    data.Add(sample, look);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void At_the_size_DocuSpend_opens_at_the_statement_screen_has_nothing_to_scroll(string sample, LookChoice look) => Headless.Run(() =>
    {
        var (window, _) = Open(sample, look: look);

        Assert.True(Overflow(window) <= 0.5, $"The window scrolls by {Overflow(window)} points.");
        Finish(window);
    });

    [Theory]
    [MemberData(nameof(Samples))]
    public void At_that_size_each_list_has_room_for_five_rows(string sample, LookChoice look) => Headless.Run(() =>
    {
        // Row height comes from a month with rows to measure; the month under test may have none.
        var (probeWindow, probeModel) = Open("02-busy-month");
        var pitch = RowPitch(ListOf(probeWindow, probeModel.Statement.Categories));
        Finish(probeWindow);

        var (window, model) = Open(sample, look: look);
        var categories = ListOf(window, model.Statement.Categories);
        var transactions = ListOf(window, model.Statement.Transactions);

        Assert.True(categories.Bounds.Height >= WantedRows * pitch, $"The categories list is {categories.Bounds.Height} high; five rows need {WantedRows * pitch}.");
        // The transaction list shows once a category is selected; its card has the same height as the categories card.
        var card = transactions.GetVisualAncestors().OfType<Border>().First(b => b.Classes.Contains("card"));
        Assert.True(card.Bounds.Height - categories.Bounds.Y >= WantedRows * pitch);
        Finish(window);
    });

    [Fact]
    public void A_list_longer_than_its_card_scrolls_inside_it_and_the_window_stays_still() => Headless.Run(() =>
    {
        var (window, model) = Open("02-busy-month", longestCategory: true);
        var transactions = ListOf(window, model.Statement.Transactions);
        var scroller = transactions.GetVisualDescendants().OfType<ScrollViewer>().First();

        Assert.True(scroller.Extent.Height > scroller.Viewport.Height, "The list must have more than fits for this test.");
        Assert.True(Overflow(window) <= 0.5);
        var card = transactions.GetVisualAncestors().OfType<Border>().First(b => b.Classes.Contains("card"));
        Assert.True(transactions.Bounds.Height <= card.Bounds.Height);
        Finish(window);
    });

    [Fact]
    public void A_taller_window_gives_the_lists_more_rows_and_still_has_nothing_to_scroll() => Headless.Run(() =>
    {
        var (window, model) = Open("02-busy-month", longestCategory: true);
        var normal = ListOf(window, model.Statement.Transactions).Bounds.Height;
        var chart = window.BalanceLine.Bounds.Height;
        Finish(window);

        var (tall, tallModel) = Open("02-busy-month", height: 1200, longestCategory: true);

        Assert.True(ListOf(tall, tallModel.Statement.Transactions).Bounds.Height > normal);
        Assert.Equal(chart, tall.BalanceLine.Bounds.Height);
        Assert.True(Overflow(tall) <= 0.5);
        Finish(tall);
    });

    [Fact]
    public void At_the_smallest_size_the_window_scrolls_and_the_header_stays_whole_inside_the_width() => Headless.Run(() =>
    {
        var (window, _) = Open("02-busy-month", width: 720, height: 520);

        Assert.True(Overflow(window) > 0, "A window this small should scroll.");
        var width = window.ContentScroller.Viewport.Width;
        foreach (var name in new[] { "ScreenSwitch", "OpenButton", "LookSwitch" })
        {
            var control = window.FindControl<Control>(name)!;
            var left = control.TranslatePoint(default, window.ContentScroller)!.Value.X;
            Assert.True(left >= 0 && left + control.Bounds.Width <= width + 0.5, $"{name} reaches outside the window.");
        }
        Finish(window);
    });

    [Fact]
    public void On_the_rules_screen_the_card_fills_the_height_and_its_list_scrolls_inside_it() => Headless.Run(() =>
    {
        var (window, model) = Open("01-typical-month");
        Headless.ShowRules(model);
        var form = model.RuleForm;
        for (var i = 0; i < 200; i++)
        {
            form.SelectedKind = ConditionKind.Text;
            form.Text = "pravidlo " + i;
            form.SelectedCategory = form.Categories[0];
            form.SelectedSubcategory = form.Subcategories[0];
            model.AddRuleCommand.Execute(null);
        }
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();

        var list = window.FindControl<ItemsControl>("RuleList")!;
        var scroller = list.GetVisualAncestors().OfType<ScrollViewer>().First();
        var card = scroller.GetVisualAncestors().OfType<Border>().First(b => b.Classes.Contains("card"));

        Assert.True(Overflow(window) <= 0.5);
        Assert.True(scroller.Extent.Height > scroller.Viewport.Height);
        Assert.True(scroller.Bounds.Height > 0.6 * card.Bounds.Height, "The list doesn't fill its card.");
        Finish(window);
    });
}
