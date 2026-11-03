using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DocuSpend.Categorization.Model;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// The open row and its details, in the real window (work/045-transaction-details/spec.md). What the details say is
/// tested in DocuSpend.Presentation.Tests; these check opening, folding, the keyboard and the layout.
/// </summary>
public sealed class TransactionDetailsWindowTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("docuspend-details-window-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    /// <summary>Opens the sample and shows the category with the most transactions, or the one given.</summary>
    private (MainWindow Window, MainViewModel Model) Start(string sample = "02-busy-month", CategoryId? category = null)
    {
        var model = new MainViewModel(new RuleStore(Path.Join(_folder, "rules.json")), new SettingsStore(Path.Join(_folder, "settings.json")));
        model.SelectedLook = model.Looks.Single(l => l.Choice == LookChoice.Light);
        var window = new MainWindow(model);
        window.Show();
        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample(sample)));
        model.Statement.SelectedCategory = category ?? model.Statement.Categories.MaxBy(c => c.Count)!.Category;
        Dispatcher.UIThread.RunJobs();
        return (window, model);
    }

    private static void Finish(Window window)
    {
        window.Close();
        Application.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;
    }

    private static ListBox List(Window window) => window.FindControl<ListBox>("TransactionList")!;

    private static ListBoxItem RowItem(Window window, int row)
    {
        var list = List(window);
        list.ScrollIntoView(row);
        Dispatcher.UIThread.RunJobs();
        return (ListBoxItem)list.ContainerFromIndex(row)!;
    }

    private static ItemsControl Details(ListBoxItem item) =>
        item.GetVisualDescendants().OfType<ItemsControl>().Single(c => c.Name == "TransactionDetails");

    private static bool IsOpen(ListBoxItem item) => item.IsSelected && Details(item).IsEffectivelyVisible;

    /// <summary>A mouse click in the middle of a control. Headless hit testing follows the last drawn frame, so one is drawn first.</summary>
    private static void Click(Window window, Control control)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Clicks a row on its description, in its first line, away from its buttons.</summary>
    private static void ClickRow(Window window, MainViewModel model, int row)
    {
        var item = RowItem(window, row);
        var description = item.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == model.Statement.Transactions[row].Description);
        Click(window, description);
    }

    private static void Key_(Window window, Key key, PhysicalKey physical)
    {
        window.KeyPress(key, RawInputModifiers.None, physical, null);
        window.KeyRelease(key, RawInputModifiers.None, physical, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static object? Focused(Window window) => window.FocusManager!.GetFocusedElement();

    private static List<string?> DetailLabels(ListBoxItem item) =>
        Details(item).GetVisualDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains("detailLabel")).Select(t => t.Text).ToList();

    // Opening and folding

    [Fact]
    public void Nothing_is_open_at_first_and_a_click_opens_a_row_with_its_details() => Headless.Run(() =>
    {
        var (window, model) = Start();
        Assert.All(Enumerable.Range(0, model.Statement.Transactions.Count), i => Assert.False(IsOpen(RowItem(window, i))));

        ClickRow(window, model, 1);

        var item = RowItem(window, 1);
        Assert.True(IsOpen(item));
        Assert.Equal(model.Statement.Transactions[1], model.Statement.SelectedTransaction);
        Assert.Equal(model.Statement.Transactions[1].Details.Select(d => d.Label), DetailLabels(item));
        Finish(window);
    });

    [Fact]
    public void A_click_on_another_row_moves_the_open_row() => Headless.Run(() =>
    {
        var (window, model) = Start();
        ClickRow(window, model, 1);

        ClickRow(window, model, 3);

        Assert.False(IsOpen(RowItem(window, 1)));
        Assert.True(IsOpen(RowItem(window, 3)));
        Finish(window);
    });

    [Fact]
    public void A_second_click_folds_the_open_row_and_nothing_is_selected() => Headless.Run(() =>
    {
        var (window, model) = Start();
        var folded = RowItem(window, 1).Bounds.Height;
        ClickRow(window, model, 1);
        Assert.True(RowItem(window, 1).Bounds.Height > folded);

        ClickRow(window, model, 1);

        Assert.False(IsOpen(RowItem(window, 1)));
        Assert.Null(model.Statement.SelectedTransaction);
        Assert.Equal(folded, RowItem(window, 1).Bounds.Height);
        Finish(window);
    });

    [Fact]
    public void A_click_on_Zaradit_opens_the_box_and_the_row_stays_open() => Headless.Run(() =>
    {
        var (window, model) = Start();
        ClickRow(window, model, 1);
        var categorize = RowItem(window, 1).GetVisualDescendants().OfType<Button>().Single(b => b.Classes.Contains("categorize"));

        Click(window, categorize);

        Assert.NotNull(model.Categorizing);
        Assert.Equal(model.Statement.Transactions[1], model.Statement.SelectedTransaction);
        model.CancelCategorizeCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.True(IsOpen(RowItem(window, 1)));
        Finish(window);
    });

    [Fact]
    public void An_open_hand_chosen_row_offers_to_undo_the_choice() => Headless.Run(() =>
    {
        var (window, model) = Start("01-typical-month", new CategoryId("online-shopping"));
        model.CategorizeCommand.Execute(model.Statement.Transactions[0]);
        var form = model.Categorizing!.Form;
        form.SelectedCategory = form.Categories.Single(c => c.Id == new CategoryId("online-shopping"));
        form.SelectedSubcategory = form.Subcategories[0];
        model.ConfirmCategorizeCommand.Execute(null);
        model.ConfirmPrivacyCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        var row = model.Statement.Transactions.Select((r, i) => (r, i)).Single(x => x.r.ByHand).i;

        ClickRow(window, model, row);

        var item = RowItem(window, row);
        Assert.True(IsOpen(item));
        Assert.Contains(item.GetVisualDescendants().OfType<Button>(), b => b.Classes.Contains("undo") && b.IsEffectivelyVisible);
        Finish(window);
    });

    [Fact]
    public void A_row_near_the_bottom_is_fully_in_view_once_open() => Headless.Run(() =>
    {
        var (window, model) = Start();
        var last = model.Statement.Transactions.Count - 1;

        ClickRow(window, model, last);

        var item = RowItem(window, last);
        var viewer = List(window).GetVisualDescendants().OfType<ScrollViewer>().First();
        var top = item.TranslatePoint(default, viewer)!.Value.Y;
        Assert.True(IsOpen(item));
        Assert.True(top >= 0 && top + item.Bounds.Height <= viewer.Viewport.Height + 0.5,
            $"The row runs from {top} to {top + item.Bounds.Height} in a list {viewer.Viewport.Height} high.");
        Finish(window);
    });

    [Fact]
    public void A_row_taller_than_the_list_shows_its_first_line() => Headless.Run(() =>
    {
        var (window, model) = Start();
        List(window).MaxHeight = 120;
        Dispatcher.UIThread.RunJobs();
        var last = model.Statement.Transactions.Count - 1;

        ClickRow(window, model, last);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        var item = RowItem(window, last);
        var viewer = List(window).GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.True(item.Bounds.Height > viewer.Viewport.Height);
        var top = item.TranslatePoint(default, viewer)!.Value.Y;
        Assert.True(top >= -0.5 && top < viewer.Viewport.Height / 2, $"The row's top is at {top} in a list {viewer.Viewport.Height} high.");
        Finish(window);
    });

    // The keyboard

    [Fact]
    public void Down_and_Up_move_the_open_row() => Headless.Run(() =>
    {
        var (window, model) = Start();
        ClickRow(window, model, 1);

        Key_(window, Key.Down, PhysicalKey.ArrowDown);
        Assert.True(IsOpen(RowItem(window, 2)));
        Assert.False(IsOpen(RowItem(window, 1)));

        Key_(window, Key.Up, PhysicalKey.ArrowUp);
        Assert.True(IsOpen(RowItem(window, 1)));
        Assert.False(IsOpen(RowItem(window, 2)));
        Finish(window);
    });

    [Fact]
    public void Escape_folds_the_open_row_and_Space_or_Enter_open_it_again() => Headless.Run(() =>
    {
        var (window, model) = Start();
        ClickRow(window, model, 2);

        Key_(window, Key.Escape, PhysicalKey.Escape);
        Assert.Null(model.Statement.SelectedTransaction);
        Assert.Same(RowItem(window, 2), Focused(window));

        Key_(window, Key.Space, PhysicalKey.Space);
        Assert.True(IsOpen(RowItem(window, 2)));
        Key_(window, Key.Space, PhysicalKey.Space);
        Assert.False(IsOpen(RowItem(window, 2)));

        Key_(window, Key.Enter, PhysicalKey.Enter);
        Assert.True(IsOpen(RowItem(window, 2)));
        Key_(window, Key.Enter, PhysicalKey.Enter);
        Assert.False(IsOpen(RowItem(window, 2)));
        Finish(window);
    });

    [Fact]
    public void Escape_with_the_box_open_closes_only_the_box() => Headless.Run(() =>
    {
        var (window, model) = Start();
        ClickRow(window, model, 1);
        model.CategorizeCommand.Execute(model.Statement.Transactions[1]);
        Dispatcher.UIThread.RunJobs();

        Key_(window, Key.Escape, PhysicalKey.Escape);

        Assert.Null(model.Categorizing);
        Assert.True(IsOpen(RowItem(window, 1)));
        Finish(window);
    });

    [Fact]
    public void Tab_from_the_open_row_reaches_Zaradit() => Headless.Run(() =>
    {
        var (window, model) = Start();
        ClickRow(window, model, 1);
        Assert.Same(RowItem(window, 1), Focused(window));

        Key_(window, Key.Tab, PhysicalKey.Tab);
        var button = Assert.IsType<Button>(Focused(window));
        Assert.Equal(Texts.Categorize, button.Content);

        window.KeyPress(Key.Tab, RawInputModifiers.Shift, PhysicalKey.Tab, null);
        window.KeyRelease(Key.Tab, RawInputModifiers.Shift, PhysicalKey.Tab, null);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(RowItem(window, 1), Focused(window));

        Key_(window, Key.Tab, PhysicalKey.Tab);
        Key_(window, Key.Tab, PhysicalKey.Tab);
        var after = Assert.IsAssignableFrom<Control>(Focused(window));
        Assert.Null(after.FindAncestorOfType<ListBox>());
        Assert.True(IsOpen(RowItem(window, 1)));
        Finish(window);
    });

    [Fact]
    public void Tab_from_an_open_hand_chosen_row_reaches_both_buttons() => Headless.Run(() =>
    {
        var (window, model) = Start("01-typical-month", new CategoryId("online-shopping"));
        model.CategorizeCommand.Execute(model.Statement.Transactions[0]);
        var form = model.Categorizing!.Form;
        form.SelectedCategory = form.Categories.Single(c => c.Id == new CategoryId("online-shopping"));
        form.SelectedSubcategory = form.Subcategories[0];
        model.ConfirmCategorizeCommand.Execute(null);
        model.ConfirmPrivacyCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        var row = model.Statement.Transactions.Select((r, i) => (r, i)).Single(x => x.r.ByHand).i;
        ClickRow(window, model, row);

        Key_(window, Key.Tab, PhysicalKey.Tab);
        Assert.Equal(Texts.UndoHandChoice, Assert.IsType<Button>(Focused(window)).Content);
        Key_(window, Key.Tab, PhysicalKey.Tab);
        Assert.Equal(Texts.Categorize, Assert.IsType<Button>(Focused(window)).Content);
        Finish(window);
    });

    // The layout

    [Fact]
    public void The_labels_share_one_width_and_a_long_purpose_wraps_in_full() => Headless.Run(() =>
    {
        var (window, model) = Start("03-awkward-month", null);
        var statement = model.Statement;
        var roof = statement.Categorized!.Transactions.Select((t, i) => (t, i))
            .Single(x => x.t.Transaction.PaymentPurpose?.Contains("strechy", StringComparison.Ordinal) == true);
        statement.SelectedCategory = roof.t.Placement.Category;
        Dispatcher.UIThread.RunJobs();
        var row = statement.Transactions.Select((r, i) => (r, i)).Single(x => x.r.Index == roof.i).i;

        ClickRow(window, model, row);

        var item = RowItem(window, row);
        var lines = Details(item).GetVisualDescendants().OfType<Grid>().Where(g => g.Classes.Contains("detail")).ToList();
        Assert.Single(lines.Select(g => g.ColumnDefinitions[0].ActualWidth).Distinct());
        var purpose = Details(item).GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == roof.t.Transaction.PaymentPurpose);
        var label = Details(item).GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("detailLabel"));
        Assert.True(purpose.Bounds.Height > 1.5 * label.Bounds.Height, "The long purpose doesn't wrap.");
        var right = purpose.TranslatePoint(new Point(purpose.Bounds.Width, 0), item)!.Value.X;
        Assert.True(right <= item.Bounds.Width, $"The purpose runs to {right}, past the row's {item.Bounds.Width}.");
        Finish(window);
    });
}
