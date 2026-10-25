using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using DocuSpend.Presentation;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// The real window, from its compiled XAML, driven without a screen. These tests show what interface-level testing
/// with Avalonia looks like; the behavior itself is tested in DocuSpend.Presentation.Tests.
/// </summary>
public sealed class MainWindowTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private (MainWindow Window, MainViewModel Model) Open()
    {
        var model = new MainViewModel(new RuleStore(Path.Combine(_folder, "rules.json")));
        Headless.Wait(model.OpenFileAsync(Sample("01-typical-month")));
        var window = new MainWindow(model);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, model);
    }

    private static IEnumerable<T> All<T>(Control root) => root.GetLogicalDescendants().OfType<T>();

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
    public void A_rule_typed_into_the_form_is_added_and_listed() => Headless.Run(() =>
    {
        var (window, model) = Open();
        var form = model.RuleForm;
        var boxes = All<ComboBox>(window).ToList();

        All<TextBox>(window).Single(t => t.IsEffectivelyVisible).Text = "NETFLIX";
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

    internal static string Sample(string name) => Path.Combine(SamplesDir(), name + ".pdf");

    internal static string SamplesDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DocuSpend.slnx")))
                return Path.Combine(dir.FullName, "samples", "public");
        }
        throw new InvalidOperationException("Repository root (DocuSpend.slnx) not found.");
    }
}
