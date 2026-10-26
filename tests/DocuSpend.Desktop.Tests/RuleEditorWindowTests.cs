using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using DocuSpend.Presentation;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// The rule editor in the real window (work/024-edit-rules/spec.md). What the view models do is tested in
/// DocuSpend.Presentation.Tests; these catch what only the window shows.
/// </summary>
public sealed class RuleEditorWindowTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("docuspend-editor-window-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private (MainWindow Window, MainViewModel Model) StartWithStatement()
    {
        var model = new MainViewModel(new RuleStore(Path.Combine(_folder, "rules.json")), example: MainWindowTests.Sample("01-typical-month"));
        var window = new MainWindow(model);
        window.Show();
        Headless.Wait(model.OpenExampleCommand.ExecuteAsync(null));
        Dispatcher.UIThread.RunJobs();
        return (window, model);
    }

    private static Control Named(Window window, string name) => window.FindControl<Control>(name)!;

    [Fact]
    public void Each_kind_of_condition_shows_its_own_fields_only() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        string[] fields = ["TextFields", "AccountField", "AmountFields", "TypeChoices", "SymbolField"];
        var expected = new Dictionary<ConditionKind, string>
        {
            [ConditionKind.Text] = "TextFields",
            [ConditionKind.CounterpartyName] = "TextFields",
            [ConditionKind.CounterpartyAccount] = "AccountField",
            [ConditionKind.Amount] = "AmountFields",
            [ConditionKind.Type] = "TypeChoices",
            [ConditionKind.VariableSymbol] = "SymbolField",
        };

        foreach (var (kind, field) in expected)
        {
            model.RuleForm.SelectedKind = kind;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([field], fields.Where(f => Named(window, f).IsEffectivelyVisible));
        }
        window.Close();
    });

    [Fact]
    public void Transaction_types_are_tick_boxes_with_slovak_names() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        model.RuleForm.SelectedKind = ConditionKind.Type;
        Dispatcher.UIThread.RunJobs();

        var boxes = Named(window, "TypeChoices").GetLogicalDescendants().OfType<CheckBox>().ToList();
        Assert.Equal(model.RuleForm.Types.Select(t => t.Label), boxes.Select(b => b.Content as string));

        boxes[0].IsChecked = true;
        boxes[2].IsChecked = true;
        Assert.Equal([true, false, true], model.RuleForm.Types.Take(3).Select(t => t.IsChecked));
        window.Close();
    });
}
