using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform.Storage;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DocuSpend.Categorization.Model;
using DocuSpend.Presentation;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// The statement screen and the rules screen in the real window (work/021-rules-screen/spec.md). What the choice
/// does is tested in DocuSpend.Presentation.Tests; these catch what only the window shows.
/// </summary>
public sealed class ScreensWindowTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("docuspend-screens-window-").FullName;

    private string RulesFile => Path.Join(_folder, "rules.json");

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private (MainWindow Window, MainViewModel Model) StartWithStatement(string sample = "02-busy-month")
    {
        var model = new MainViewModel(new RuleStore(RulesFile));
        var window = new MainWindow(model) { Width = 1280, Height = 940 };
        window.Show();
        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample(sample)));
        Dispatcher.UIThread.RunJobs();
        return (window, model);
    }

    private static Control Named(Window window, string name) => window.FindControl<Control>(name)!;

    private static void Choose(MainWindow window, MainViewModel model, AppScreen screen)
    {
        model.SelectedScreen = model.Screens.Single(s => s.Screen == screen);
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    private static bool Shows(Control root, string text) =>
        root.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text == text && t.IsEffectivelyVisible);

    private static ListBox ListOf(MainWindow window, object items) =>
        window.GetVisualDescendants().OfType<ListBox>().Single(l => ReferenceEquals(l.ItemsSource, items));

    [Fact]
    public void The_start_screen_has_no_switch_between_screens() => Headless.Run(() =>
    {
        var model = new MainViewModel(new RuleStore(RulesFile));
        var window = new MainWindow(model);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.False(window.ScreenSwitch.IsEffectivelyVisible);
        Assert.False(window.StatementScreen.IsEffectivelyVisible);
        Assert.False(window.RulesScreen.IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void With_a_statement_the_switch_offers_both_screens_and_Vypis_is_chosen() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();

        Assert.True(window.ScreenSwitch.IsEffectivelyVisible);
        var labels = window.ScreenSwitch.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Equal(["Výpis", "Pravidlá"], labels);
        Assert.Equal(AppScreen.Statement, model.SelectedScreen.Screen);
        window.Close();
    });

    [Fact]
    public void The_statement_screen_has_no_rule_parts_and_the_rules_screen_has_no_statement_parts() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();

        Assert.True(window.StatementScreen.IsEffectivelyVisible);
        Assert.False(window.RulesScreen.IsEffectivelyVisible);
        Assert.True(Shows(window, Texts.SpendingChart));
        Assert.True(Shows(window, Texts.BalanceChart));
        Assert.True(Shows(window, Texts.Categories));
        Assert.True(Shows(window, Texts.Transactions));
        Assert.True(Shows(window, Texts.SpendingTotal));
        Assert.False(Shows(window, Texts.NewRule));
        Assert.False(window.RuleViewSwitch.IsEffectivelyVisible);
        Assert.False(Shows(window, Texts.Rules));
        Assert.False(Shows(window, Texts.BuiltInRules));

        Choose(window, model, AppScreen.Rules);

        Assert.False(window.StatementScreen.IsEffectivelyVisible);
        Assert.True(window.RulesScreen.IsEffectivelyVisible);
        Assert.True(Shows(window, Texts.NewRule));
        Assert.True(window.RuleViewSwitch.IsEffectivelyVisible);
        Assert.False(Shows(window, Texts.SpendingChart));
        Assert.False(Shows(window, Texts.BalanceChart));
        Assert.False(Shows(window, Texts.Categories));
        Assert.False(Shows(window, Texts.Transactions));
        Assert.False(Shows(window, Texts.SpendingTotal));
        window.Close();
    });

    [Fact]
    public void The_header_keeps_its_controls_in_the_same_places_on_both_screens() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        string[] names = ["ScreenSwitch", "OpenButton", "LookSwitch"];
        Rect At(string name) => new(Named(window, name).TranslatePoint(default, window)!.Value, Named(window, name).Bounds.Size);
        var onStatement = names.Select(At).ToList();

        Choose(window, model, AppScreen.Rules);

        Assert.All(names, n => Assert.True(Named(window, n).IsEffectivelyVisible));
        Assert.Equal(onStatement, names.Select(At).ToList());
        window.Close();
    });

    [Fact]
    public void The_period_and_the_example_badge_show_on_the_rules_screen_too() => Headless.Run(() =>
    {
        var model = new MainViewModel(new RuleStore(RulesFile), example: MainWindowTests.Sample("01-typical-month"));
        var window = new MainWindow(model) { Width = 1280, Height = 940 };
        window.Show();
        Headless.Wait(model.OpenExampleCommand.ExecuteAsync(null));
        Choose(window, model, AppScreen.Rules);

        Assert.True(Shows(window, model.Statement.Period));
        Assert.True(Shows(window, Texts.ExampleLabel));
        window.Close();
    });

    [Fact]
    public void Clicking_the_pills_switches_the_screen_and_the_chosen_one_does_nothing() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        var pills = window.ScreenSwitch.GetVisualDescendants().OfType<ListBoxItem>().ToList();
        void Click(Control pill)
        {
            var centre = pill.TranslatePoint(new Point(pill.Bounds.Width / 2, pill.Bounds.Height / 2), window)!.Value;
            window.MouseDown(centre, MouseButton.Left);
            window.MouseUp(centre, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        Click(pills[1]);
        Assert.Equal(AppScreen.Rules, model.SelectedScreen.Screen);
        Click(pills[1]);
        Assert.Equal(AppScreen.Rules, model.SelectedScreen.Screen);
        Click(pills[0]);
        Assert.Equal(AppScreen.Statement, model.SelectedScreen.Screen);
        window.Close();
    });

    [Fact]
    public void A_failed_open_from_the_rules_screen_stays_there_with_the_message() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        Choose(window, model, AppScreen.Rules);

        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample("07-not-a-statement")));
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.RulesScreen.IsEffectivelyVisible);
        Assert.True(Named(window, "OpenMessage").IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void A_file_dropped_on_the_rules_screen_opens_and_shows_the_statement_screen() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        Choose(window, model, AppScreen.Rules);
        var before = model.Statement.Categorized;
        using var transfer = new DataTransfer();
        var file = window.StorageProvider.TryGetFileFromPathAsync(MainWindowTests.Sample("01-typical-month"));
        Headless.Wait(file);
        transfer.Add(DataTransferItem.CreateFile(file.Result!));
        var at = new Point(640, 400);

        window.DragDrop(at, RawDragEventType.DragEnter, transfer, DragDropEffects.Copy, RawInputModifiers.None);
        Assert.True(Named(window, "DropOverlay").IsEffectivelyVisible);
        window.DragDrop(at, RawDragEventType.Drop, transfer, DragDropEffects.Copy, RawInputModifiers.None);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (ReferenceEquals(before, model.Statement.Categorized) && DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(1);
        }
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.StatementScreen.IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void Ctrl_O_is_bound_on_both_screens_because_it_belongs_to_the_window() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        Choose(window, model, AppScreen.Rules);

        var binding = Assert.Single(window.KeyBindings, b => b.Gesture == new KeyGesture(Key.O, KeyModifiers.Control));
        Assert.Same(model.OpenCommand, binding.Command);
        window.Close();
    });

    [Fact]
    public void The_open_and_rules_not_loaded_messages_show_on_both_screens_until_closed() => Headless.Run(() =>
    {
        File.WriteAllText(RulesFile, "{ damaged");
        var (window, model) = StartWithStatement();
        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample("07-not-a-statement")));
        Dispatcher.UIThread.RunJobs();

        Assert.True(Named(window, "OpenMessage").IsEffectivelyVisible);
        Assert.True(Named(window, "RulesMessage").IsEffectivelyVisible);
        Choose(window, model, AppScreen.Rules);
        Assert.True(Named(window, "OpenMessage").IsEffectivelyVisible);
        Assert.True(Named(window, "RulesMessage").IsEffectivelyVisible);

        model.CloseMessageCommand.Execute(null);
        model.CloseRulesMessageCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.False(Named(window, "OpenMessage").IsEffectivelyVisible);
        Assert.False(Named(window, "RulesMessage").IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void The_not_saving_line_shows_on_the_rules_screen_only() => Headless.Run(() =>
    {
        File.WriteAllText(RulesFile, """{ "version": 2 }""");
        var (window, model) = StartWithStatement();

        Assert.False(Named(window, "RulesNotSaving").IsEffectivelyVisible);
        Choose(window, model, AppScreen.Rules);
        Assert.True(Named(window, "RulesNotSaving").IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void The_changes_messages_stay_on_the_rules_screen() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        Choose(window, model, AppScreen.Rules);
        model.RuleForm.SelectedKind = ConditionKind.Text;
        model.RuleForm.Text = "NETFLIX";
        model.RuleForm.SelectedCategory = model.RuleForm.Categories.Single(c => c.Id == new CategoryId("leisure"));
        model.RuleForm.SelectedSubcategory = model.RuleForm.Subcategories[0];
        model.AddRuleCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(model.RuleForm.Message);
        Assert.True(Shows(window, model.RuleForm.Message!));

        Choose(window, model, AppScreen.Statement);

        Assert.False(Shows(window, model.RuleForm.Message!));
        window.Close();
    });

    [Fact]
    public void Both_charts_have_their_series_and_a_size_after_a_round_trip() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();

        Choose(window, model, AppScreen.Rules);
        Choose(window, model, AppScreen.Statement);
        AvaloniaHeadlessPlatformForceRender();

        Assert.Equal(model.Statement.Slices.Count, window.SpendingPie.Series!.Count());
        Assert.NotEmpty(window.BalanceLine.Series!);
        Assert.True(window.SpendingPie.Bounds.Width > 0 && window.SpendingPie.Bounds.Height > 0);
        Assert.True(window.BalanceLine.Bounds.Width > 0 && window.BalanceLine.Bounds.Height > 0);
        window.Close();
    });

    private static void AvaloniaHeadlessPlatformForceRender()
    {
        Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public void A_shown_hover_card_is_gone_after_switching_screens() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        window.ShowBalanceDay(model.Statement.BalancePoints[0], new Point(400, 500));
        Assert.True(window.BalanceCard.IsVisible);

        Choose(window, model, AppScreen.Rules);

        Assert.False(window.RingCard.IsVisible);
        Assert.False(window.BalanceCard.IsVisible);
        Assert.False(window.HoverDot.IsVisible);
        window.Close();
    });

    [Fact]
    public void The_selected_category_its_transactions_and_the_scroll_positions_survive_a_round_trip() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        var statement = model.Statement;
        statement.SelectedRow = statement.Categories.MaxBy(row =>
        {
            statement.SelectedRow = row;
            return statement.Transactions.Count;
        })!;
        Dispatcher.UIThread.RunJobs();
        var category = statement.SelectedCategory;
        var transactions = ListOf(window, statement.Transactions).GetVisualDescendants().OfType<ScrollViewer>().First();
        var categories = ListOf(window, statement.Categories).GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.True(transactions.Extent.Height > transactions.Viewport.Height, "The list must have more than fits for this test.");
        transactions.Offset = new Vector(0, 40);
        Dispatcher.UIThread.RunJobs();
        var transactionOffset = transactions.Offset;
        var categoryOffset = categories.Offset;

        Choose(window, model, AppScreen.Rules);
        Choose(window, model, AppScreen.Statement);

        Assert.Equal(category, statement.SelectedCategory);
        Assert.NotNull(statement.SelectedRow);
        Assert.Equal(transactionOffset, transactions.Offset);
        Assert.Equal(categoryOffset, categories.Offset);
        window.Close();
    });

    [Fact]
    public void A_half_typed_rule_and_the_chosen_view_of_the_rules_card_survive_a_round_trip() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        Choose(window, model, AppScreen.Rules);
        model.RuleForm.SelectedKind = ConditionKind.VariableSymbol;
        model.RuleForm.Symbol = "123456";
        model.SelectedRuleView = model.RuleViews[1];
        Dispatcher.UIThread.RunJobs();

        Choose(window, model, AppScreen.Statement);
        Choose(window, model, AppScreen.Rules);

        var symbol = Named(window, "SymbolField") as TextBox;
        Assert.Equal("123456", symbol!.Text);
        Assert.True(symbol.IsEffectivelyVisible);
        Assert.True(Named(window, "BuiltInRules").IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void The_switch_cannot_be_reached_under_the_deletion_pop_up() => Headless.Run(() =>
    {
        var (window, model) = StartWithStatement();
        Choose(window, model, AppScreen.Rules);
        model.RuleForm.SelectedKind = ConditionKind.Text;
        model.RuleForm.Text = "NETFLIX";
        model.RuleForm.SelectedCategory = model.RuleForm.Categories.Single(c => c.Id == new CategoryId("leisure"));
        model.RuleForm.SelectedSubcategory = model.RuleForm.Subcategories[0];
        model.AddRuleCommand.Execute(null);
        model.AskToDeleteCommand.Execute(model.Rules[0]);
        Dispatcher.UIThread.RunJobs();

        var pill = window.ScreenSwitch.GetVisualDescendants().OfType<ListBoxItem>().First();
        var centre = pill.TranslatePoint(new Point(pill.Bounds.Width / 2, pill.Bounds.Height / 2), window)!.Value;
        AvaloniaHeadlessPlatformForceRender();
        var hit = window.InputHitTest(centre) as Visual;

        Assert.NotNull(hit);
        Assert.Contains(Named(window, "DeleteDialog"), hit!.GetSelfAndVisualAncestors());
        window.Close();
    });
}
