using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
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

    [Fact]
    public void The_help_mark_by_my_rules_explains_the_numbers_on_hover_and_on_keyboard_focus() => Headless.Run(() =>
    {
        var (window, _) = StartWithStatement();
        var help = (Button)Named(window, "RulesHelp");

        Assert.Equal(Texts.PriorityHelp, ToolTip.GetTip(help));
        Assert.False(ToolTip.GetIsOpen(help));

        help.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.True(ToolTip.GetIsOpen(help));

        Assert.True(window.GetLogicalDescendants().OfType<TextBox>().First(t => t.IsEffectivelyVisible).Focus(NavigationMethod.Tab));
        Dispatcher.UIThread.RunJobs();
        Assert.False(ToolTip.GetIsOpen(help));
        window.Close();
    });

    private static void AddRule(MainViewModel model, string text)
    {
        model.RuleForm.Text = text;
        model.RuleForm.SelectedCategory = model.RuleForm.Categories[0];
        model.RuleForm.SelectedSubcategory = model.RuleForm.Subcategories[0];
        model.AddRuleCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
    }

    private static Button RowButton(Window window, int row, string label) =>
        Named(window, "RuleList").GetLogicalDescendants().OfType<Border>().Where(b => b.Classes.Contains("ruleRow")).ElementAt(row)
            .GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == label && b.IsEffectivelyVisible);

    private static void Press(Button button)
    {
        button.Command!.Execute(button.CommandParameter);
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public void Upravit_turns_the_form_into_the_editor_of_that_rule_and_marks_its_row() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        AddRule(model, "Pekáreň");
        AddRule(model, "Kino");
        Assert.False(Named(window, "PriorityRow").IsEffectivelyVisible);

        Press(RowButton(window, 1, "Upraviť"));

        Assert.Equal("Upraviť pravidlo", ((TextBlock)Named(window, "FormHeading")).Text);
        Assert.True(Named(window, "PriorityRow").IsEffectivelyVisible);
        Assert.Equal("1", ((TextBox)Named(window, "PriorityField")).Text);
        Assert.Equal(Texts.PriorityHelp, ToolTip.GetTip(Named(window, "PriorityHelp")));
        Assert.True(Named(window, "EditButtons").IsEffectivelyVisible);
        Assert.False(Named(window, "AddRuleButton").IsEffectivelyVisible);
        var rows = Named(window, "RuleList").GetLogicalDescendants().OfType<Border>().Where(b => b.Classes.Contains("ruleRow")).ToList();
        Assert.Equal([false, true], rows.Select(r => r.Classes.Contains("editing")));
        Assert.Equal("Pekáreň", model.RuleForm.Text);

        var cancel = Named(window, "EditButtons").GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == "Zrušiť");
        Press(cancel);
        Assert.Equal("Nové pravidlo", ((TextBlock)Named(window, "FormHeading")).Text);
        Assert.True(Named(window, "AddRuleButton").IsEffectivelyVisible);
        window.Close();
    });

    private static bool Shows(Window window, string text) =>
        window.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text == text && t.IsEffectivelyVisible);

    [Fact]
    public void Odstranit_asks_in_the_row_and_Odstranit_again_deletes() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        AddRule(model, "Pekáreň");
        AddRule(model, "Kino");

        Press(RowButton(window, 1, "Odstrániť"));

        Assert.True(Shows(window, "Odstrániť toto pravidlo?"));
        Assert.Equal(2, model.Rules.Count);
        Assert.Throws<InvalidOperationException>(() => RowButton(window, 1, "Upraviť"));
        Press(RowButton(window, 1, "Odstrániť"));

        Assert.Equal(["Text obsahuje „Kino“"], model.Rules.Select(r => r.Condition));
        Assert.False(Shows(window, "Odstrániť toto pravidlo?"));
        Assert.True(Named(window, "RulesListMessage").IsEffectivelyVisible);
        Assert.True(Shows(window, "Pravidlo je odstránené."));
        window.Close();
    });

    [Fact]
    public void Ponechat_and_Escape_take_the_question_back() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        AddRule(model, "Pekáreň");

        Press(RowButton(window, 0, "Odstrániť"));
        Press(RowButton(window, 0, "Ponechať"));
        Assert.False(Shows(window, "Odstrániť toto pravidlo?"));

        Press(RowButton(window, 0, "Odstrániť"));
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Dispatcher.UIThread.RunJobs();
        Assert.False(Shows(window, "Odstrániť toto pravidlo?"));
        Assert.Single(model.Rules);
        window.Close();
    });

    [Fact]
    public void Vstavane_pravidla_is_closed_at_start_and_opened_lists_every_default_rule_with_256_and_no_buttons() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        var section = (Expander)Named(window, "BuiltInRules");

        Assert.False(section.IsExpanded);
        Assert.Equal("Vstavané pravidlá", section.Header);

        section.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();

        var list = Named(window, "BuiltInRuleList");
        Assert.True(list.IsEffectivelyVisible);
        var rows = list.GetLogicalDescendants().OfType<Border>().Where(b => b.Classes.Contains("ruleRow")).ToList();
        Assert.Equal(model.Statement.Config.DefaultRules.Count, rows.Count);
        Assert.All(rows, r => Assert.Equal("256", r.GetLogicalDescendants().OfType<TextBlock>().First().Text));
        Assert.Empty(list.GetLogicalDescendants().OfType<Button>());
        window.Close();
    });

    [Fact]
    public void Each_rule_in_the_list_shows_its_number() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        model.RuleForm.Text = "Pekáreň";
        model.RuleForm.SelectedCategory = model.RuleForm.Categories[0];
        model.RuleForm.SelectedSubcategory = model.RuleForm.Subcategories[0];
        model.AddRuleCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var numbers = window.GetLogicalDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains("priority") && t.IsEffectivelyVisible);
        Assert.Equal(["1"], numbers.Select(t => t.Text));
        window.Close();
    });
}
