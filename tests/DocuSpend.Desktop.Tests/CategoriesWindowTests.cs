using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DocuSpend.Categorization.Model;
using DocuSpend.Presentation;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// The "Kategórie" card in the real window (work/025-own-categories/spec.md). What the view models do is tested in
/// DocuSpend.Presentation.Tests; these catch what only the window shows.
/// </summary>
public sealed class CategoriesWindowTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("docuspend-categories-window-").FullName;

    private string RulesFile => Path.Join(_folder, "rules.json");

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private (MainWindow Window, MainViewModel Model) Start(double width = 1280, double height = 940)
    {
        var model = new MainViewModel(new RuleStore(RulesFile), example: MainWindowTests.Sample("01-typical-month"));
        var window = new MainWindow(model) { Width = width, Height = height };
        window.Show();
        Headless.Wait(model.OpenExampleCommand.ExecuteAsync(null));
        Headless.ShowRules(model);
        Dispatcher.UIThread.RunJobs();
        return (window, model);
    }

    private static Control Named(Window window, string name) => window.FindControl<Control>(name)!;

    private static IEnumerable<T> All<T>(Window window, string name) where T : Control =>
        window.GetVisualDescendants().OfType<T>().Where(c => c.Name == name);

    private static T Row<T>(MainWindow window, string category, string name) where T : Control
    {
        var model = (MainViewModel)window.DataContext!;
        var index = model.Categories.Rows.ToList().FindIndex(r => r.Name == category);
        var container = window.CategoryList.ContainerFromIndex(index)!;
        return container.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
    }

    private static void Add(MainWindow window, string name, CategoryRole role = CategoryRole.Spending)
    {
        var model = (MainViewModel)window.DataContext!;
        window.CategoryNameField.Text = name;
        model.Categories.SelectedRole = model.Categories.Roles.Single(r => r.Role == role);
        window.AddCategoryButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public void The_card_is_under_the_form_with_the_add_fields_and_shows_only_on_the_rules_screen() => Headless.Run(() =>
    {
        var (window, model) = Start();

        var form = Named(window, "FormHeading");
        var card = window.CategoriesCard;
        Assert.True(card.IsEffectivelyVisible);
        Assert.True(card.TranslatePoint(default, window)!.Value.Y > form.TranslatePoint(default, window)!.Value.Y);
        var formCard = form.GetVisualAncestors().OfType<Border>().First(b => b.Classes.Contains("card"));
        Assert.Equal(formCard.TranslatePoint(default, window)!.Value.X, card.TranslatePoint(default, window)!.Value.X, 1);
        Assert.True(window.CategoryNameField.IsEffectivelyVisible);
        Assert.True(window.AddCategoryButton.IsEffectivelyVisible);
        Assert.True(window.RoleChoice.IsEffectivelyVisible);
        Assert.Equal(Texts.RoleHelp, ToolTip.GetTip(window.RoleHelp));
        Assert.Equal(16, window.CategoryList.ItemCount);

        model.SelectedScreen = model.Screens[0];
        Dispatcher.UIThread.RunJobs();
        Assert.False(card.IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void Adding_a_category_with_the_button_shows_its_row_and_brings_it_into_view() => Headless.Run(() =>
    {
        var (window, model) = Start();
        for (var i = 1; i <= 12; i++)
            Add(window, $"Vlastná {i}");
        Dispatcher.UIThread.RunJobs();

        Add(window, "Šport");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(29, window.CategoryList.ItemCount);
        Assert.Equal("Kategória „Šport“ je pridaná.", model.Categories.Message);
        Assert.True(window.CategoriesMessage.IsEffectivelyVisible);
        Assert.Equal("", window.CategoryNameField.Text);
        var row = window.CategoryList.ContainerFromIndex(model.Categories.Rows.ToList().FindIndex(r => r.Name == "Šport"))!;
        var top = row.TranslatePoint(default, window.CategoriesScroll)!.Value.Y;
        Assert.InRange(top, 0, window.CategoriesScroll.Viewport.Height);
        window.Close();
    });

    [Fact]
    public void A_refused_name_shows_its_reason_under_the_field_and_stays_typed() => Headless.Run(() =>
    {
        var (window, _) = Start();

        Add(window, "Bývanie");

        Assert.True(window.CategoryNameError.IsEffectivelyVisible);
        Assert.Equal("Taký názov už existuje.", window.CategoryNameError.Text);
        Assert.Equal("Bývanie", window.CategoryNameField.Text);
        window.Close();
    });

    [Fact]
    public void The_line_for_a_sub_category_opens_with_its_field_focused_and_closes_when_another_opens_or_is_cancelled() => Headless.Run(() =>
    {
        var (window, model) = Start();
        Assert.DoesNotContain(All<StackPanel>(window, "SubcategoryLine"), l => l.IsEffectivelyVisible);

        model.Categories.OpenLineCommand.Execute(model.Categories.Rows.Single(r => r.Name == "Stravovanie"));
        Dispatcher.UIThread.RunJobs();
        var field = Row<TextBox>(window, "Stravovanie", "SubcategoryNameField");
        Assert.True(Row<StackPanel>(window, "Stravovanie", "SubcategoryLine").IsEffectivelyVisible);
        Assert.True(field.IsFocused);

        model.Categories.OpenLineCommand.Execute(model.Categories.Rows.Single(r => r.Name == "Bývanie"));
        Dispatcher.UIThread.RunJobs();
        Assert.False(Row<StackPanel>(window, "Stravovanie", "SubcategoryLine").IsEffectivelyVisible);
        Assert.True(Row<StackPanel>(window, "Bývanie", "SubcategoryLine").IsEffectivelyVisible);

        Row<Button>(window, "Bývanie", "CancelSubcategoryButton").Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain(All<StackPanel>(window, "SubcategoryLine"), l => l.IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void A_refused_sub_category_name_shows_its_reason_under_its_field_and_the_line_stays_open() => Headless.Run(() =>
    {
        var (window, model) = Start();
        model.Categories.OpenLineCommand.Execute(model.Categories.Rows.Single(r => r.Name == "Stravovanie"));
        Dispatcher.UIThread.RunJobs();

        Row<TextBox>(window, "Stravovanie", "SubcategoryNameField").Text = "Potraviny";
        Row<Button>(window, "Stravovanie", "AddSubcategoryButton").Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var reason = Row<TextBlock>(window, "Stravovanie", "SubcategoryError");
        Assert.True(reason.IsEffectivelyVisible);
        Assert.Equal("Taký názov už existuje.", reason.Text);
        Assert.True(Row<StackPanel>(window, "Stravovanie", "SubcategoryLine").IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void The_button_to_add_a_sub_category_is_on_every_row_except_the_two_uncategorized_ones() => Headless.Run(() =>
    {
        var (window, model) = Start();

        var buttons = window.CategoryList.GetVisualDescendants().OfType<Button>()
            .Where(b => b.Content as string == Texts.AddSubcategory && b.IsEffectivelyVisible).ToList();

        Assert.Equal(14, buttons.Count);
        Assert.Equal(["Nezaradené výdavky", "Nezaradené príjmy"], model.Categories.Rows.TakeLast(2).Select(r => r.Name));
        window.Close();
    });

    [Fact]
    public void The_name_fields_take_at_most_forty_characters() => Headless.Run(() =>
    {
        var (window, model) = Start();
        model.Categories.OpenLineCommand.Execute(model.Categories.Rows[0]);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Texts.MaxNameLength, window.CategoryNameField.MaxLength);
        Assert.Equal(Texts.MaxNameLength, Row<TextBox>(window, model.Categories.Rows[0].Name, "SubcategoryNameField").MaxLength);
        window.Close();
    });

    [Fact]
    public void With_many_categories_the_card_scrolls_inside_itself_and_the_window_does_not_scroll() => Headless.Run(() =>
    {
        var (window, _) = Start();
        for (var i = 1; i <= 30; i++)
            Add(window, $"Vlastná kategória {i}");
        Dispatcher.UIThread.RunJobs();

        var scroll = window.CategoriesScroll;
        Assert.True(scroll.Extent.Height > scroll.Viewport.Height);
        Assert.True(window.ContentScroller.Extent.Height <= window.ContentScroller.Viewport.Height + 0.5);
        Assert.True(scroll.Extent.Width <= scroll.Viewport.Width + 0.5);
        window.Close();
    });

    [Fact]
    public void While_changes_are_not_saved_the_card_says_so() => Headless.Run(() =>
    {
        File.WriteAllText(RulesFile, """{ "version": 2 }""");
        var (window, _) = Start();

        Assert.True(window.CategoriesNotSaving.IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void The_card_is_as_it_was_after_a_switch_to_the_statement_and_back() => Headless.Run(() =>
    {
        var (window, model) = Start();
        window.CategoryNameField.Text = "Šport";
        model.Categories.SelectedRole = model.Categories.Roles[2];
        model.Categories.OpenLineCommand.Execute(model.Categories.Rows.Single(r => r.Name == "Stravovanie"));
        Dispatcher.UIThread.RunJobs();
        Row<TextBox>(window, "Stravovanie", "SubcategoryNameField").Text = "Obedy";
        window.CategoriesScroll.Offset = new Vector(0, 40);
        Dispatcher.UIThread.RunJobs();

        model.SelectedScreen = model.Screens[0];
        Dispatcher.UIThread.RunJobs();
        Headless.ShowRules(model);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Šport", window.CategoryNameField.Text);
        Assert.Equal(CategoryRole.Excluded, model.Categories.SelectedRole.Role);
        Assert.Equal("Obedy", Row<TextBox>(window, "Stravovanie", "SubcategoryNameField").Text);
        Assert.True(Row<StackPanel>(window, "Stravovanie", "SubcategoryLine").IsEffectivelyVisible);
        Assert.Equal(40, window.CategoriesScroll.Offset.Y);
        window.Close();
    });

    [Fact]
    public void Enter_in_the_name_field_adds_the_category_and_not_a_rule() => Headless.Run(() =>
    {
        var (window, model) = Start();
        model.RuleForm.Text = "minimarket";
        window.CategoryNameField.Text = "Šport";
        window.CategoryNameField.Focus();
        Dispatcher.UIThread.RunJobs();

        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(model.Categories.Rows, r => r.Name == "Šport");
        Assert.Empty(model.Rules);
        window.Close();
    });

    [Fact]
    public void Enter_in_the_sub_category_field_adds_the_sub_category_and_not_a_rule() => Headless.Run(() =>
    {
        var (window, model) = Start();
        model.Categories.OpenLineCommand.Execute(model.Categories.Rows.Single(r => r.Name == "Stravovanie"));
        Dispatcher.UIThread.RunJobs();
        var field = Row<TextBox>(window, "Stravovanie", "SubcategoryNameField");
        field.Text = "Obedy";
        field.Focus();

        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(model.Categories.Rows.Single(r => r.Name == "Stravovanie").Subcategories, s => s.Name == "Obedy");
        Assert.Empty(model.Rules);
        window.Close();
    });

    [Fact]
    public void A_new_sub_category_is_a_choice_in_the_rule_forms_lists_at_once() => Headless.Run(() =>
    {
        var (window, model) = Start();
        model.RuleForm.SelectedCategory = model.RuleForm.Categories.Single(c => c.Name == "Stravovanie");
        model.RuleForm.SelectedSubcategory = model.RuleForm.Subcategories[0];
        Dispatcher.UIThread.RunJobs();

        model.Categories.OpenLineCommand.Execute(model.Categories.Rows.Single(r => r.Name == "Stravovanie"));
        Dispatcher.UIThread.RunJobs();
        Row<TextBox>(window, "Stravovanie", "SubcategoryNameField").Text = "Obedy v práci";
        Row<Button>(window, "Stravovanie", "AddSubcategoryButton").Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Stravovanie", model.RuleForm.SelectedCategory?.Name);
        Assert.Equal("Potraviny", model.RuleForm.SelectedSubcategory?.Name);
        Assert.Contains(model.RuleForm.Subcategories, s => s.Name == "Obedy v práci");
        window.Close();
    });
}
