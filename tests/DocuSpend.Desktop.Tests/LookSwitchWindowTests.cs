using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// The look switch and the rules card's switch, in the real window (work/019-look-switch-scroll-bars/spec.md): the same
/// shape on every screen, with the chosen choice's text centred.
/// </summary>
public sealed class LookSwitchWindowTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("docuspend-look-switch-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    public static TheoryData<LookChoice> Variants => new([LookChoice.Light, LookChoice.Dark]);

    private (MainWindow Window, MainViewModel Model) Start(LookChoice look)
    {
        var model = new MainViewModel(new RuleStore(Path.Combine(_folder, "rules.json")));
        model.SelectedLook = model.Looks.Single(l => l.Choice == look);
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

    internal static List<ListBoxItem> Choices(ListBox @switch) => @switch.GetVisualDescendants().OfType<ListBoxItem>().ToList();

    /// <summary>The chosen choice's pill and where its text sits in it.</summary>
    private sealed record Pill(Size Size, double Top, double Bottom, double TextHeight, double TextNatural);

    private static Pill Chosen(ListBox @switch)
    {
        var item = Choices(@switch).Single(i => i.IsSelected);
        var text = item.GetVisualDescendants().OfType<TextBlock>().First();
        var top = text.TranslatePoint(new Point(0, 0), item)!.Value.Y;
        return new Pill(item.Bounds.Size, top, item.Bounds.Height - top - text.Bounds.Height, text.Bounds.Height, text.DesiredSize.Height);
    }

    private static void AssertCentred(Pill pill)
    {
        // Text drawn at the top of a stretched text block looks off-centre, so the block must be only as tall as its text.
        Assert.Equal(pill.TextNatural, pill.TextHeight, tolerance: 1);
        Assert.Equal(pill.Top, pill.Bottom, tolerance: 1);
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void The_look_switch_keeps_its_shape_when_a_statement_is_opened(LookChoice look) => Headless.Run(() =>
    {
        var (window, model) = Start(look);
        var startHeight = window.LookSwitch.Bounds.Height;
        var start = Chosen(window.LookSwitch);

        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample("01-typical-month")));
        Dispatcher.UIThread.RunJobs();
        var open = Chosen(window.LookSwitch);

        Assert.Equal(startHeight, window.LookSwitch.Bounds.Height, tolerance: 1);
        Assert.Equal(start.Size, open.Size);
        AssertCentred(start);
        AssertCentred(open);
        Finish(window);
    });

    [Fact]
    public void With_a_statement_open_the_look_switch_sits_centred_in_the_header_row() => Headless.Run(() =>
    {
        var (window, model) = Start(LookChoice.Light);
        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample("01-typical-month")));
        Dispatcher.UIThread.RunJobs();

        var row = window.OpenButton.GetVisualAncestors().OfType<Grid>().First();
        var top = window.LookSwitch.TranslatePoint(new Point(0, 0), row)!.Value.Y;
        var bottom = row.Bounds.Height - top - window.LookSwitch.Bounds.Height;

        Assert.True(window.LookSwitch.Bounds.Height < row.Bounds.Height, "The header row is taller than the switch with a statement open.");
        Assert.Equal(top, bottom, tolerance: 1);
        Finish(window);
    });

    [Theory]
    [MemberData(nameof(Variants))]
    public void The_rules_switch_has_its_text_centred(LookChoice look) => Headless.Run(() =>
    {
        var (window, model) = Start(look);
        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample("01-typical-month")));
        Dispatcher.UIThread.RunJobs();

        AssertCentred(Chosen(window.RuleViewSwitch));
        Finish(window);
    });
}
