using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using DocuSpend.Categorization.Model;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

namespace DocuSpend.Desktop.Tests;

/// <summary>The window in both looks, and switching between them while it shows something (spec: charts, choosing).</summary>
public sealed class LookTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private (MainWindow Window, MainViewModel Model) Open(LookChoice choice)
    {
        var model = new MainViewModel(new RuleStore(Path.Combine(_folder, "rules.json")));
        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample("01-typical-month")));
        model.SelectedLook = model.Looks.Single(l => l.Choice == choice);
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

    private static Color CardBackground(Window window) =>
        ((ISolidColorBrush)window.GetLogicalDescendants().OfType<Border>().First(b => b.Classes.Contains("card")).Background!).Color;

    private static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string Hex(SkiaSharp.SKColor color) => $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";

    public static TheoryData<string> Variants => new(["light", "dark"]);

    private static (LookChoice Variant, Palette Palette) Of(string look) =>
        look == "dark" ? (LookChoice.Dark, Palette.Dark) : (LookChoice.Light, Palette.Light);

    [Theory]
    [MemberData(nameof(Variants))]
    public void The_ring_has_a_hole_and_each_category_its_own_colour(string look) => Headless.Run(() =>
    {
        var (window, model) = Open(Of(look).Variant);

        var ring = window.SpendingPie.Series!.Cast<PieSeries<double>>().ToList();

        Assert.Equal(model.Statement.Slices.Count, ring.Count);
        Assert.All(ring, s => Assert.True(s.InnerRadius > 0));
        Assert.Equal(model.Statement.Slices.Select(s => s.Color), ring.Select(s => Hex(((SolidColorPaint)s.Fill!).Color)));
        Finish(window);
    });

    [Theory]
    [MemberData(nameof(Variants))]
    public void Cards_and_the_balance_line_take_the_looks_colours(string look) => Headless.Run(() =>
    {
        var (variant, palette) = Of(look);
        var (window, _) = Open(variant);

        Assert.Equal(palette.Card, Hex(CardBackground(window)));
        var line = Assert.IsType<StepLineSeries<DateTimePoint>>(Assert.Single(window.BalanceLine.Series!));
        Assert.Equal(palette.Accent, Hex(((SolidColorPaint)line.Stroke!).Color));
        Assert.Equal(0, line.GeometrySize);
        Finish(window);
    });

    [Fact]
    public void Switching_the_look_recolours_everything_and_keeps_what_the_user_was_doing() => Headless.Run(() =>
    {
        var (window, model) = Open(LookChoice.Light);
        model.Statement.SelectedCategory = new CategoryId("housing");
        model.RuleForm.Text = "rozpísané pravidlo";

        model.SelectedLook = model.Looks.Single(l => l.Choice == LookChoice.Dark);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Palette.Dark.Card, Hex(CardBackground(window)));
        var line = (StepLineSeries<DateTimePoint>)window.BalanceLine.Series!.Single();
        Assert.Equal(Palette.Dark.Accent, Hex(((SolidColorPaint)line.Stroke!).Color));
        Assert.Equal(new CategoryId("housing"), model.Statement.SelectedCategory);
        Assert.Equal("rozpísané pravidlo", model.RuleForm.Text);
        Assert.True(model.HasStatement);
        Finish(window);
    });

    [Theory]
    [InlineData(LookChoice.System, "Default")]
    [InlineData(LookChoice.Light, "Light")]
    [InlineData(LookChoice.Dark, "Dark")]
    public void The_choice_in_the_header_sets_the_applications_look(LookChoice choice, string variant) => Headless.Run(() =>
    {
        var (window, model) = Open(LookChoice.Light);

        window.LookSwitch.SelectedItem = model.Looks.Single(l => l.Choice == choice);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(choice, model.Look);
        Assert.Equal(variant, Application.Current!.RequestedThemeVariant!.Key.ToString());
        Finish(window);
    });

    [Fact]
    public void A_row_of_the_legend_selects_its_category() => Headless.Run(() =>
    {
        var (window, model) = Open(LookChoice.Light);
        var rows = window.Legend.GetLogicalDescendants().OfType<Button>().ToList();

        Assert.Equal(model.Statement.Slices.Count, rows.Count);
        rows[1].Command!.Execute(rows[1].CommandParameter);

        Assert.Equal(model.Statement.Slices[1].Category, model.Statement.SelectedCategory);
        Finish(window);
    });
}
