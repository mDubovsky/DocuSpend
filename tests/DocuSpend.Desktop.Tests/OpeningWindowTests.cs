using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DocuSpend.Presentation;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// The start screen, the header and dropping, in the real window (spec: starting; opening a statement). What opening
/// does is tested in DocuSpend.Presentation.Tests; these catch what only the window shows.
/// </summary>
public sealed class OpeningWindowTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private (MainWindow Window, MainViewModel Model) Start()
    {
        var model = new MainViewModel(new RuleStore(Path.Combine(_folder, "rules.json")),
            example: MainWindowTests.Sample("01-typical-month"));
        var window = new MainWindow(model);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, model);
    }

    private static IEnumerable<T> All<T>(Control root) => root.GetLogicalDescendants().OfType<T>();

    private static Button ButtonWith(Control root, string text) =>
        All<Button>(root).Single(b => b.Content is string content && content == text && b.IsEffectivelyVisible);

    private static bool Shows(Control root, string text) =>
        All<TextBlock>(root).Any(t => t.Text == text && t.IsEffectivelyVisible);

    /// <summary>Runs the UI thread until the condition holds, as the application's message loop would.</summary>
    private static void Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("The window never got there.");
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(1);
        }
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Files as Avalonia's storage API hands them over from Windows Explorer.</summary>
    private static DataTransfer FileTransfer(TopLevel window, params string[] paths)
    {
        var transfer = new DataTransfer();
        foreach (var path in paths)
        {
            var file = window.StorageProvider.TryGetFileFromPathAsync(path);
            Headless.Wait(file);
            transfer.Add(DataTransferItem.CreateFile(file.Result ?? throw new InvalidOperationException("No storage file for " + path)));
        }
        return transfer;
    }

    private static void Drop(MainWindow window, IDataTransfer transfer, Point at)
    {
        window.DragDrop(at, RawDragEventType.DragEnter, transfer, DragDropEffects.Copy, RawInputModifiers.None);
        window.DragDrop(at, RawDragEventType.DragOver, transfer, DragDropEffects.Copy, RawInputModifiers.None);
        window.DragDrop(at, RawDragEventType.Drop, transfer, DragDropEffects.Copy, RawInputModifiers.None);
    }

    private static readonly Point Middle = new(640, 300);

    [Fact]
    public void The_window_starts_on_the_start_screen() => Headless.Run(() =>
    {
        var (window, _) = Start();

        Assert.True(window.StartScreen.IsEffectivelyVisible);
        Assert.True(Shows(window, Texts.StartHeading));
        Assert.True(Shows(window, Texts.StartHint));
        Assert.True(Shows(window, Texts.StartPrivacy));
        Assert.Contains("primary", ButtonWith(window, Texts.OpenStatement).Classes);
        ButtonWith(window, Texts.TryExample);
        Assert.False(window.SpendingPie.IsEffectivelyVisible);
        Assert.DoesNotContain(All<Button>(window), b => b.Content is string text && text == Texts.AddRule && b.IsEffectivelyVisible);
        Assert.False(window.OpenButton.IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void Trying_the_example_shows_it_labelled() => Headless.Run(() =>
    {
        var (window, model) = Start();

        ButtonWith(window, Texts.TryExample).Command!.Execute(null);
        Until(() => model.HasStatement);

        Assert.False(window.StartScreen.IsEffectivelyVisible);
        Assert.True(window.SpendingPie.IsEffectivelyVisible);
        Assert.True(Shows(window, Texts.ExampleLabel));
        Assert.True(Shows(window, model.Statement.Period));
        Assert.True(window.OpenButton.IsEffectivelyVisible);
        Assert.DoesNotContain("primary", window.OpenButton.Classes);
        window.Close();
    });

    [Fact]
    public void A_file_that_fails_shows_its_message_and_the_close_button_removes_it() => Headless.Run(() =>
    {
        var (window, model) = Start();
        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample("01-typical-month")));

        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample("07-not-a-statement")));
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.OpenMessage.IsEffectivelyVisible);
        Assert.True(Shows(window, model.OpenMessage!));
        Assert.False(Shows(window, Texts.ExampleLabel));
        ButtonWith(window, Texts.Close).Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.False(window.OpenMessage.IsEffectivelyVisible);
        Assert.True(window.SpendingPie.IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void Ctrl_O_opens_the_open_window() => Headless.Run(() =>
    {
        var (window, model) = Start();

        var binding = Assert.Single(window.KeyBindings, b => b.Gesture == new KeyGesture(Key.O, KeyModifiers.Control));
        Assert.Same(model.OpenCommand, binding.Command);
        window.Close();
    });

    [Fact]
    public void The_open_window_offers_pdf_files_first_and_all_files_too()
    {
        Assert.Equal([Texts.PickerPdfFiles, Texts.PickerAllFiles], StatementPicker.Filters.Select(f => f.Name));
        Assert.Equal(["*.pdf"], StatementPicker.Filters[0].Patterns!);
        Assert.Equal(["*.*"], StatementPicker.Filters[1].Patterns!);
    }

    [Fact]
    public void A_dropped_file_opens() => Headless.Run(() =>
    {
        var (window, model) = Start();

        Drop(window, FileTransfer(window, MainWindowTests.Sample("02-busy-month")), Middle);
        Until(() => model.HasStatement);

        Assert.True(window.SpendingPie.IsEffectivelyVisible);
        Assert.False(window.DropOverlay.IsVisible);
        Assert.False(model.IsExample);
        window.Close();
    });

    [Fact]
    public void Dropped_text_gives_its_message() => Headless.Run(() =>
    {
        var (window, model) = Start();
        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.CreateText("nejaký text"));

        Drop(window, transfer, Middle);
        Until(() => model.OpenMessage is not null);

        Assert.Equal(Texts.DropNotAFile, model.OpenMessage);
        Assert.False(model.HasStatement);
        window.Close();
    });

    [Fact]
    public void Several_dropped_files_give_their_message() => Headless.Run(() =>
    {
        var (window, model) = Start();

        Drop(window, FileTransfer(window, MainWindowTests.Sample("01-typical-month"), MainWindowTests.Sample("02-busy-month")), Middle);
        Until(() => model.OpenMessage is not null);

        Assert.Equal(Texts.DropSeveralFiles, model.OpenMessage);
        Assert.False(model.HasStatement);
        window.Close();
    });

    [Fact]
    public void A_file_dropped_onto_the_rule_forms_text_box_opens_and_leaves_the_box_alone() => Headless.Run(() =>
    {
        var (window, model) = Start();
        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample("01-typical-month")));
        Dispatcher.UIThread.RunJobs();
        var box = All<TextBox>(window).First(t => t.IsEffectivelyVisible);
        box.BringIntoView();
        Dispatcher.UIThread.RunJobs();
        var at = box.TranslatePoint(new Point(10, 10), window)!.Value;
        var before = model.Statement.Categorized;

        Drop(window, FileTransfer(window, MainWindowTests.Sample("02-busy-month")), at);
        Until(() => !ReferenceEquals(before, model.Statement.Categorized));

        Assert.True(string.IsNullOrEmpty(box.Text));
        window.Close();
    });

    [Fact]
    public void Dragging_a_file_over_the_window_shows_where_to_drop_it() => Headless.Run(() =>
    {
        var (window, model) = Start();
        var transfer = FileTransfer(window, MainWindowTests.Sample("01-typical-month"));

        window.DragDrop(Middle, RawDragEventType.DragEnter, transfer, DragDropEffects.Copy, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.True(window.DropOverlay.IsVisible);
        Assert.True(Shows(window, Texts.DropHere));

        window.DragDrop(Middle, RawDragEventType.DragLeave, transfer, DragDropEffects.Copy, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.False(window.DropOverlay.IsVisible);
        Assert.False(model.IsDraggingOver);
        window.Close();
    });

}
