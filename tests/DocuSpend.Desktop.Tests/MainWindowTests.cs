using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DocuSpend.Categorization.Rules;
using DocuSpend.Presentation;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// The real window, from its compiled XAML, driven without a screen. These tests show what interface-level testing
/// with Avalonia looks like; the behavior itself is tested in DocuSpend.Presentation.Tests.
/// </summary>
public sealed class MainWindowTests : IDisposable
{
    private readonly string _folder = Path.Join(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private (MainWindow Window, MainViewModel Model) Open()
    {
        var model = new MainViewModel(new RuleStore(Path.Join(_folder, "rules.json")));
        Headless.Wait(model.OpenFileAsync(Sample("01-typical-month")));
        var window = new MainWindow(model);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, model);
    }

    private static IEnumerable<T> All<T>(Control root) => root.GetLogicalDescendants().OfType<T>();

    private (MainWindow Window, MainViewModel Model) Open(string sample)
    {
        var model = new MainViewModel(new RuleStore(Path.Join(_folder, "rules.json")));
        Headless.Wait(model.OpenFileAsync(Sample(sample)));
        var window = new MainWindow(model);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, model);
    }

    [Fact]
    public void A_statement_with_unknown_types_shows_the_note_with_its_help_mark() => Headless.Run(() =>
    {
        var (window, _) = Open("10-new-types-month");
        var note = window.FindControl<StackPanel>("UnknownTypes")!;

        Assert.True(note.IsEffectivelyVisible);
        Assert.Contains(All<TextBlock>(note), t => t.Text == "2 transakcie neznámeho typu");
        Assert.Equal(Texts.UnknownTypesHelp, ToolTip.GetTip(window.FindControl<Button>("UnknownTypesHelp")!));
        window.Close();
    });

    [Fact]
    public void A_statement_without_unknown_types_shows_no_note() => Headless.Run(() =>
    {
        var (window, _) = Open("01-typical-month");

        Assert.False(window.FindControl<StackPanel>("UnknownTypes")!.IsVisible);
        window.Close();
    });

    [Fact]
    public void The_window_loads_from_its_xaml_and_shows_the_first_sample_in_slovak() => Headless.Run(() =>
    {
        var (window, model) = Open();

        Assert.Equal(Texts.AppTitle, window.Title);
        Assert.Contains(All<TextBlock>(window), t => t.Text == model.Statement.Period);
        Assert.Contains(All<TextBlock>(window), t => t.Text == Texts.SpendingChart);
        Assert.Equal(model.Statement.Slices.Count, window.SpendingPie.Series!.Count());
        window.Close();
    });

    [Fact]
    public void Choosing_a_category_in_the_list_fills_the_transaction_list() => Headless.Run(() =>
    {
        var (window, model) = Open();
        var lists = All<ListBox>(window).ToList();
        var categories = lists.Single(l => ReferenceEquals(l.ItemsSource, model.Statement.Categories));

        categories.SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();

        var transactions = lists.Single(l => ReferenceEquals(l.ItemsSource, model.Statement.Transactions));
        Assert.True(transactions.IsVisible);
        Assert.Equal(model.Statement.Categories[0].Count, transactions.ItemCount);
        window.Close();
    });

    [Fact]
    public void A_chosen_category_that_moves_out_of_sight_is_scrolled_into_view() => Headless.Run(() =>
    {
        // The busiest sample has more categories than the card shows (work/047-largest-first/plan.md).
        var (window, model) = Open("02-busy-month");
        var view = model.Statement;
        var categories = All<ListBox>(window).Single(l => ReferenceEquals(l.ItemsSource, view.Categories));
        var scroller = categories.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.True(scroller.Extent.Height > scroller.Viewport.Height);

        // Choose the largest category, at the top of the card.
        var chosen = view.Categories[0];
        view.SelectedCategory = chosen.Category;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, scroller.Offset.Y);
        var rowHeight = categories.ContainerFromIndex(0)!.Bounds.Height;

        // Move all but its smallest transaction by hand to the smallest category that has sub-categories, so the chosen
        // one drops down the card.
        view.Config = view.Config.WithFingerprintKey(FingerprintKey.Create());
        var its = view.Categorized!.Transactions.Where(t => t.Placement.Category == chosen.Category).ToList();
        var kept = its.MinBy(t => Math.Abs(t.Transaction.Amount))!;
        var smallest = view.Categories.Last(c => view.Config.Catalog.Categories.Single(k => k.Id == c.Category).Subcategories.Count > 0);
        var target = view.Config.Catalog.Categories.Single(k => k.Id == smallest.Category).Subcategories[0].Id;
        var config = view.Config;
        foreach (var moved in its.Where(t => t != kept))
            config = config.SetHandChoice(moved.Fingerprint!.Value, target).Value!;
        view.Config = config;
        Dispatcher.UIThread.RunJobs();

        // Where it is now would be out of sight had the card stayed at the top.
        var after = view.Categories.ToList().FindIndex(c => c.Category == chosen.Category);
        Assert.True((after + 1) * rowHeight > scroller.Viewport.Height, $"row {after} of {view.Categories.Count}");
        Assert.Equal(chosen.Category, view.SelectedCategory);
        Assert.Equal(after, categories.SelectedIndex);
        var row = categories.ContainerFromIndex(after)!;
        var top = row.TranslatePoint(new Point(0, 0), scroller)!.Value.Y;
        Assert.InRange(top, 0, scroller.Viewport.Height - row.Bounds.Height);
        window.Close();
    });

    [Fact]
    public void A_rule_typed_into_the_form_is_added_and_listed() => Headless.Run(() =>
    {
        var (window, model) = Open();
        Headless.ShowRules(model);
        var form = model.RuleForm;
        var boxes = All<ComboBox>(window).ToList();

        // Within the rules screen: a screen never shown yet reports its text boxes as visible in the logical tree.
        All<TextBox>(window.RulesScreen).Single(t => t.IsEffectivelyVisible).Text = "NETFLIX";
        boxes.Single(b => ReferenceEquals(b.ItemsSource, form.Categories)).SelectedItem =
            form.Categories.Single(c => c.Name == "Zábava a oddych");
        Dispatcher.UIThread.RunJobs();
        boxes.Single(b => ReferenceEquals(b.ItemsSource, form.Subcategories)).SelectedIndex = 0;
        All<Button>(window).Single(b => b.Content is string text && text == Texts.AddRule).Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.Null(form.Error);
        Assert.Single(model.Rules);
        Assert.Contains(All<TextBlock>(window), t => t.Text == "Text obsahuje „NETFLIX“");
        window.Close();
    });

    internal static string Sample(string name) => Path.Join(SamplesDir(), name + ".pdf");

    internal static string SamplesDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Join(dir.FullName, "DocuSpend.slnx")))
                return Path.Join(dir.FullName, "samples", "public");
        }
        throw new InvalidOperationException("Repository root (DocuSpend.slnx) not found.");
    }
}
