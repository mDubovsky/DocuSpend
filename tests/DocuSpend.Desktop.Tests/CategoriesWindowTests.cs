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
/// The "Kategórie" screen in the real window (work/025-own-categories/spec.md). What the view models do is tested in
/// DocuSpend.Presentation.Tests; these catch what only the window shows.
/// </summary>
public sealed class CategoriesWindowTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("docuspend-categories-window-").FullName;

    private string RulesFile => Path.Join(_folder, "rules.json");

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private (MainWindow Window, MainViewModel Model) Start()
    {
        var model = new MainViewModel(new RuleStore(RulesFile), example: MainWindowTests.Sample("01-typical-month"));
        var window = new MainWindow(model) { Width = 1280, Height = 940 };
        window.Show();
        Headless.Wait(model.OpenExampleCommand.ExecuteAsync(null));
        ShowCategories(model);
        return (window, model);
    }

    private static void ShowCategories(MainViewModel model)
    {
        model.SelectedScreen = model.Screens.Single(s => s.Screen == AppScreen.Categories);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Choose(MainViewModel model, string category)
    {
        model.Categories.SelectedRow = model.Categories.Rows.Single(r => r.Name == category);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Add(MainWindow window, string name, CategoryRole role = CategoryRole.Spending)
    {
        var model = (MainViewModel)window.DataContext!;
        window.NewCategoryButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        window.CategoryNameField.Text = name;
        model.Categories.SelectedRole = model.Categories.Roles.Single(r => r.Role == role);
        window.AddCategoryButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public void Kategorie_is_the_third_screen_and_shows_only_its_own_cards() => Headless.Run(() =>
    {
        var (window, model) = Start();

        var labels = window.ScreenSwitch.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Equal(["Výpis", "Pravidlá", "Kategórie"], labels);
        Assert.True(window.CategoriesScreen.IsEffectivelyVisible);
        Assert.False(window.StatementScreen.IsEffectivelyVisible);
        Assert.False(window.RulesScreen.IsEffectivelyVisible);

        Headless.ShowRules(model);
        Assert.False(window.CategoriesScreen.IsEffectivelyVisible);
        Assert.True(window.RulesScreen.IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void The_list_shows_every_category_and_the_first_one_is_chosen_with_its_details() => Headless.Run(() =>
    {
        var (window, _) = Start();

        Assert.Equal(16, window.CategoryList.ItemCount);
        Assert.True(window.CategoryDetails.IsEffectivelyVisible);
        Assert.False(window.NewCategoryForm.IsEffectivelyVisible);
        Assert.Equal("Bývanie", window.CategoryDetailsName.Text);
        Assert.Equal("Počíta sa ako výdavky", window.CategoryDetailsRole.Text);
        Assert.Equal(4, window.SubcategoryList.ItemCount);
        window.Close();
    });

    [Fact]
    public void Ostatne_says_what_it_is_for() => Headless.Run(() =>
    {
        var (window, _) = Start();

        var hints = window.SubcategoryList.GetVisualDescendants().OfType<TextBlock>()
            .Where(t => t.Text == Texts.OtherHint && t.IsEffectivelyVisible).ToList();

        Assert.Single(hints);
        window.Close();
    });

    [Fact]
    public void An_uncategorized_category_says_what_it_holds_and_offers_no_sub_category() => Headless.Run(() =>
    {
        var (window, model) = Start();

        Choose(model, "Nezaradené výdavky");

        Assert.True(window.UncategorizedNote.IsEffectivelyVisible);
        Assert.False(window.SubcategoryPart.IsEffectivelyVisible);
        Assert.False(window.SubcategoryNameField.IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void Nova_kategoria_shows_the_form_with_the_cursor_in_the_name_and_Zrusit_shows_the_details_again() => Headless.Run(() =>
    {
        var (window, _) = Start();

        window.NewCategoryButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.NewCategoryForm.IsEffectivelyVisible);
        Assert.False(window.CategoryDetails.IsEffectivelyVisible);
        Assert.True(window.CategoryNameField.IsFocused);
        Assert.Equal(Texts.RoleHelp, ToolTip.GetTip(window.RoleHelp));

        window.CancelNewCategoryButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.False(window.NewCategoryForm.IsEffectivelyVisible);
        Assert.True(window.CategoryDetails.IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void Adding_a_category_chooses_it_and_brings_it_into_view_with_the_confirmation() => Headless.Run(() =>
    {
        var (window, model) = Start();
        for (var i = 1; i <= 12; i++)
            Add(window, $"Vlastná {i}");

        Add(window, "Šport", CategoryRole.Excluded);

        Assert.Equal(29, window.CategoryList.ItemCount);
        Assert.Equal("Šport", window.CategoryDetailsName.Text);
        Assert.Equal("Nepočíta sa do výdavkov", window.CategoryDetailsRole.Text);
        Assert.True(window.CategoriesMessage.IsEffectivelyVisible);
        var index = model.Categories.Rows.ToList().FindIndex(r => r.Name == "Šport");
        var row = window.CategoryList.ContainerFromIndex(index)!;
        var scroller = window.CategoryList.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.InRange(row.TranslatePoint(default, scroller)!.Value.Y, 0, scroller.Viewport.Height);
        window.Close();
    });

    [Fact]
    public void A_refused_name_shows_its_reason_under_the_field_and_the_form_stays() => Headless.Run(() =>
    {
        var (window, _) = Start();

        Add(window, "Bývanie");

        Assert.True(window.NewCategoryForm.IsEffectivelyVisible);
        Assert.True(window.CategoryNameError.IsEffectivelyVisible);
        Assert.Equal("Taký názov už existuje.", window.CategoryNameError.Text);
        Assert.Equal("Bývanie", window.CategoryNameField.Text);
        window.Close();
    });

    [Fact]
    public void A_sub_category_is_added_under_the_chosen_category_and_a_refused_one_shows_its_reason() => Headless.Run(() =>
    {
        var (window, model) = Start();
        Choose(model, "Stravovanie");

        window.SubcategoryNameField.Text = "Potraviny";
        window.AddSubcategoryButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.True(window.SubcategoryError.IsEffectivelyVisible);
        Assert.Equal("Taký názov už existuje.", window.SubcategoryError.Text);

        window.SubcategoryNameField.Text = "Obedy v práci";
        window.AddSubcategoryButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(4, window.SubcategoryList.ItemCount);
        Assert.Equal("", window.SubcategoryNameField.Text);
        Assert.True(window.CategoriesMessage.IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void The_name_fields_take_at_most_forty_characters() => Headless.Run(() =>
    {
        var (window, _) = Start();

        Assert.Equal(Texts.MaxNameLength, window.CategoryNameField.MaxLength);
        Assert.Equal(Texts.MaxNameLength, window.SubcategoryNameField.MaxLength);
        window.Close();
    });

    [Fact]
    public void With_many_categories_the_list_scrolls_inside_its_card_and_the_window_does_not_scroll() => Headless.Run(() =>
    {
        var (window, _) = Start();
        for (var i = 1; i <= 30; i++)
            Add(window, $"Vlastná kategória {i}");

        var scroller = window.CategoryList.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.True(scroller.Extent.Height > scroller.Viewport.Height);
        Assert.True(window.ContentScroller.Extent.Height <= window.ContentScroller.Viewport.Height + 0.5);
        window.Close();
    });

    [Fact]
    public void While_changes_are_not_saved_the_screen_says_so() => Headless.Run(() =>
    {
        File.WriteAllText(RulesFile, """{ "version": 99 }""");
        var (window, _) = Start();

        Assert.True(window.CategoriesNotSaving.IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void The_screen_is_as_it_was_after_a_switch_to_another_screen_and_back() => Headless.Run(() =>
    {
        var (window, model) = Start();
        Choose(model, "Stravovanie");
        window.SubcategoryNameField.Text = "Obedy";

        model.SelectedScreen = model.Screens[0];
        Dispatcher.UIThread.RunJobs();
        ShowCategories(model);

        Assert.Equal("Stravovanie", window.CategoryDetailsName.Text);
        Assert.Equal("Obedy", window.SubcategoryNameField.Text);
        Assert.Same(model.Categories.SelectedRow, window.CategoryList.SelectedItem);
        window.Close();
    });

    [Fact]
    public void Enter_in_the_name_field_adds_the_category_and_not_a_rule() => Headless.Run(() =>
    {
        var (window, model) = Start();
        model.RuleForm.Text = "minimarket";
        window.NewCategoryButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        window.CategoryNameField.Text = "Šport";
        window.CategoryNameField.Focus();

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
        model.RuleForm.Text = "minimarket";
        Choose(model, "Stravovanie");
        window.SubcategoryNameField.Text = "Obedy";
        window.SubcategoryNameField.Focus();

        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(model.Categories.Rows.Single(r => r.Name == "Stravovanie").Subcategories, s => s.Name == "Obedy");
        Assert.Empty(model.Rules);
        window.Close();
    });

    [Fact]
    public void A_new_sub_category_is_a_choice_in_the_rule_form_and_a_chosen_target_stays() => Headless.Run(() =>
    {
        var (window, model) = Start();
        Headless.ShowRules(model);
        model.RuleForm.SelectedCategory = model.RuleForm.Categories.Single(c => c.Name == "Stravovanie");
        model.RuleForm.SelectedSubcategory = model.RuleForm.Subcategories[0];
        Dispatcher.UIThread.RunJobs();

        ShowCategories(model);
        Choose(model, "Stravovanie");
        window.SubcategoryNameField.Text = "Obedy v práci";
        window.AddSubcategoryButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Headless.ShowRules(model);

        Assert.Equal("Stravovanie", model.RuleForm.SelectedCategory?.Name);
        Assert.Equal("Potraviny", model.RuleForm.SelectedSubcategory?.Name);
        Assert.Contains(model.RuleForm.Subcategories, s => s.Name == "Obedy v práci");
        window.Close();
    });
}
