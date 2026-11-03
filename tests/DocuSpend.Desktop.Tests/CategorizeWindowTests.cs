using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DocuSpend.Categorization.Model;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// Categorizing a transaction from the list, in the real window (work/022-categorize-from-list/spec.md). What the view
/// models do is tested in DocuSpend.Presentation.Tests; these catch what only the window shows.
/// </summary>
public sealed class CategorizeWindowTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("docuspend-categorize-window-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private (MainWindow Window, MainViewModel Model) Start(LookChoice look = LookChoice.Light)
    {
        var model = new MainViewModel(new RuleStore(Path.Join(_folder, "rules.json")), new SettingsStore(Path.Join(_folder, "settings.json")));
        model.SelectedLook = model.Looks.Single(l => l.Choice == look);
        var window = new MainWindow(model);
        window.Show();
        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample("01-typical-month")));
        model.Statement.SelectedCategory = new CategoryId("online-shopping");
        Dispatcher.UIThread.RunJobs();
        return (window, model);
    }

    private static void Finish(Window window)
    {
        window.Close();
        Application.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;
    }

    private static Control Named(Window window, string name) => window.FindControl<Control>(name)!;

    private static ListBoxItem RowItem(Window window, int row) =>
        ((ListBox)Named(window, "TransactionList")).GetVisualDescendants().OfType<ListBoxItem>().ElementAt(row);

    private static List<Button> VisibleButtons(Control row) =>
        row.GetVisualDescendants().OfType<Button>().Where(b => b.IsEffectivelyVisible).ToList();

    private static void Press(Button button)
    {
        button.Command!.Execute(button.CommandParameter);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Escape(Window window)
    {
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Select(MainViewModel model, int row)
    {
        model.Statement.SelectedTransaction = model.Statement.Transactions[row];
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Chooses the target in the open box and confirms "Len túto transakciu", with the box about fingerprints.</summary>
    private static void JustThis(MainViewModel model, SubcategoryId target)
    {
        var form = model.Categorizing!.Form;
        form.SelectedCategory = form.Categories.Single(c => c.Subcategories.Any(s => s.Id == target));
        form.SelectedSubcategory = form.Subcategories.Single(s => s.Id == target);
        model.ConfirmCategorizeCommand.Execute(null);
        model.ConfirmPrivacyCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public void Only_the_selected_row_shows_Zaradit() => Headless.Run(() =>
    {
        var (window, model) = Start();
        Assert.All(Enumerable.Range(0, model.Statement.Transactions.Count), i => Assert.Empty(VisibleButtons(RowItem(window, i))));

        Select(model, 0);

        Assert.Equal(["Zaradiť"], VisibleButtons(RowItem(window, 0)).Select(b => b.Content as string));
        Finish(window);
    });

    [Fact]
    public void Zaradit_opens_the_box_over_the_window_with_the_transaction_and_the_focus() => Headless.Run(() =>
    {
        var (window, model) = Start();
        Select(model, 0);

        Press(VisibleButtons(RowItem(window, 0)).Single());

        Assert.True(Named(window, "CategorizeDialog").IsEffectivelyVisible);
        Assert.Equal("NETFLIX.COM", ((TextBlock)Named(window, "CategorizeDialogDescription")).Text);
        Assert.True(Named(window, "CategorizeDialogBox").IsFocused);
        Assert.True(((RadioButton)Named(window, "JustThisChoice")).IsChecked);
        Assert.False(Named(window, "CategorizeCondition").IsEffectivelyVisible);
        Finish(window);
    });

    [Fact]
    public void Choosing_a_rule_shows_the_condition_filled_in_from_the_transaction() => Headless.Run(() =>
    {
        var (window, model) = Start();
        model.CategorizeCommand.Execute(model.Statement.Transactions[0]);
        Dispatcher.UIThread.RunJobs();

        ((RadioButton)Named(window, "ByRuleChoice")).IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(model.Categorizing!.ByRule);
        Assert.True(Named(window, "CategorizeCondition").IsEffectivelyVisible);
        var fields = window.FindControl<ConditionFields>("CategorizeConditionFields")!;
        Assert.Equal("NETFLIX.COM", ((TextBox)fields.FindControl<Control>("TextField")!).Text);
        Finish(window);
    });

    [Fact]
    public void A_refused_condition_shows_its_reason_in_the_box() => Headless.Run(() =>
    {
        var (window, model) = Start();
        model.CategorizeCommand.Execute(model.Statement.Transactions[0]);
        model.Categorizing!.ByRule = true;
        model.Categorizing.Form.Text = "SPOTIFY";
        Dispatcher.UIThread.RunJobs();

        Press((Button)Named(window, "CategorizeDialogConfirm"));

        Assert.True(Named(window, "CategorizeError").IsEffectivelyVisible);
        Assert.True(Named(window, "CategorizeDialog").IsEffectivelyVisible);
        Finish(window);
    });

    [Fact]
    public void Escape_closes_the_box_on_top_first() => Headless.Run(() =>
    {
        var (window, model) = Start();
        model.CategorizeCommand.Execute(model.Statement.Transactions[0]);
        model.Categorizing!.Form.SelectedCategory = model.Categorizing.Form.Categories[0];
        model.Categorizing.Form.SelectedSubcategory = model.Categorizing.Form.Subcategories[0];
        Press((Button)Named(window, "CategorizeDialogConfirm"));
        Assert.True(Named(window, "PrivacyDialog").IsEffectivelyVisible);
        Assert.True(Named(window, "PrivacyDialogBox").IsFocused);

        Escape(window);
        Assert.False(Named(window, "PrivacyDialog").IsEffectivelyVisible);
        Assert.True(Named(window, "CategorizeDialog").IsEffectivelyVisible);

        Escape(window);
        Assert.False(Named(window, "CategorizeDialog").IsEffectivelyVisible);
        Assert.Empty(model.Statement.Config.HandChoices);
        Finish(window);
    });

    [Fact]
    public void The_box_about_fingerprints_shows_its_text_and_an_unticked_box() => Headless.Run(() =>
    {
        var (window, model) = Start();
        model.CategorizeCommand.Execute(model.Statement.Transactions[0]);
        model.Categorizing!.Form.SelectedCategory = model.Categorizing.Form.Categories[0];
        model.Categorizing.Form.SelectedSubcategory = model.Categorizing.Form.Subcategories[0];
        model.ConfirmCategorizeCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(
            [Texts.FingerprintWhat, Texts.FingerprintLimits, Texts.FingerprintWhere],
            new[] { "PrivacyWhat", "PrivacyLimits", "PrivacyWhere" }.Select(n => ((TextBlock)Named(window, n)).Text));
        var tick = (CheckBox)Named(window, "DontShowAgain");
        Assert.Equal(Texts.DontShowAgain, tick.Content);
        Assert.False(tick.IsChecked);
        tick.IsChecked = true;
        Assert.True(model.Categorizing.HideNoteNextTime);
        Finish(window);
    });

    public static TheoryData<LookChoice> Looks => new() { LookChoice.Light, LookChoice.Dark };

    [Theory]
    [MemberData(nameof(Looks))]
    public void The_box_about_fingerprints_fits_the_default_window(LookChoice look) => Headless.Run(() =>
    {
        var (window, model) = Start(look);
        model.CategorizeCommand.Execute(model.Statement.Transactions[0]);
        model.Categorizing!.Form.SelectedCategory = model.Categorizing.Form.Categories[0];
        model.Categorizing.Form.SelectedSubcategory = model.Categorizing.Form.Subcategories[0];
        model.ConfirmCategorizeCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var box = Named(window, "PrivacyDialogBox");
        var top = box.TranslatePoint(default, window)!.Value;
        Assert.Equal((1280d, 940d), (window.Bounds.Width, window.Bounds.Height));
        Assert.True(top.Y >= 0 && top.Y + box.Bounds.Height <= window.Bounds.Height, $"The box runs from {top.Y} to {top.Y + box.Bounds.Height}.");
        Assert.True(top.X >= 0 && top.X + box.Bounds.Width <= window.Bounds.Width);
        Assert.All(box.GetVisualDescendants().OfType<TextBlock>().Where(t => t.TextWrapping == Avalonia.Media.TextWrapping.Wrap),
            t => Assert.DoesNotContain(t.TextLayout.TextLines, l => l.HasOverflowed));
        Finish(window);
    });

    [Fact]
    public void A_hand_chosen_row_shows_rucne_and_its_undo_button_when_selected() => Headless.Run(() =>
    {
        var (window, model) = Start();
        model.CategorizeCommand.Execute(model.Statement.Transactions[0]);
        JustThis(model, new SubcategoryId("online-shopping.other"));
        model.Statement.SelectedCategory = new CategoryId("online-shopping");
        Dispatcher.UIThread.RunJobs();

        var row = RowItem(window, model.Statement.Transactions.ToList().FindIndex(r => r.ByHand));
        var tag = row.GetVisualDescendants().OfType<Border>().Single(b => b.Classes.Contains("byHand"));
        Assert.True(tag.IsEffectivelyVisible);
        Assert.Equal(Texts.ByHandTooltip, ToolTip.GetTip((Control)tag.GetVisualParent()!));
        Assert.Empty(VisibleButtons(row));

        row.IsSelected = true;
        Dispatcher.UIThread.RunJobs();
        Assert.False(tag.IsEffectivelyVisible);
        Assert.Equal(["× Zrušiť zaradenie", "Zaradiť"], VisibleButtons(row).Select(b => b.Content as string));

        Press(VisibleButtons(row)[0]);
        Assert.Empty(model.Statement.Config.HandChoices);
        Assert.True(Named(window, "TransactionsMessage").IsEffectivelyVisible);
        Finish(window);
    });

    [Fact]
    public void Pointing_at_rucne_turns_it_into_the_undo_button() => Headless.Run(() =>
    {
        var (window, model) = Start();
        model.CategorizeCommand.Execute(model.Statement.Transactions[0]);
        JustThis(model, new SubcategoryId("online-shopping.other"));
        model.Statement.SelectedCategory = new CategoryId("online-shopping");
        Dispatcher.UIThread.RunJobs();
        var row = RowItem(window, model.Statement.Transactions.ToList().FindIndex(r => r.ByHand));
        var tag = row.GetVisualDescendants().OfType<Border>().Single(b => b.Classes.Contains("byHand"));

        window.MouseMove(LookSwitchWindowTests.CentreInView(window, tag));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["× Zrušiť zaradenie"], VisibleButtons(row).Select(b => b.Content as string));
        Finish(window);
    });

    [Fact]
    public void A_row_placed_by_a_rule_has_no_tag() => Headless.Run(() =>
    {
        var (window, _) = Start();

        Assert.All(((ListBox)Named(window, "TransactionList")).GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("byHand")),
            tag => Assert.False(tag.IsEffectivelyVisible));
        Finish(window);
    });
}
