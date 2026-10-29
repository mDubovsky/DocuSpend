using DocuSpend.Categorization.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>Adding categories and sub-categories on the "Kategórie" card (work/025-own-categories/spec.md).</summary>
public sealed class CategoriesAddingTests : IDisposable
{
    private readonly string _folder = Path.Join(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    private string RulesFile => Path.Join(_folder, "rules.json");

    public void Dispose()
    {
        if (File.Exists(_folder))
            File.Delete(_folder);
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private async Task<MainViewModel> Start(string sample = "01-typical-month")
    {
        var main = new MainViewModel(new RuleStore(RulesFile));
        await main.OpenFileAsync(Samples.Path(sample));
        Assert.True(main.HasStatement);
        return main;
    }

    private static void AddCategory(MainViewModel main, string name, CategoryRole role = CategoryRole.Spending)
    {
        main.Categories.StartNewCategoryCommand.Execute(null);
        main.Categories.NewName = name;
        main.Categories.SelectedRole = main.Categories.Roles.Single(r => r.Role == role);
        main.AddCategoryCommand.Execute(null);
    }

    private static void Choose(MainViewModel main, string category) =>
        main.Categories.SelectedRow = main.Categories.Rows.Single(r => r.Name == category);

    private static void AddSubcategory(MainViewModel main, string category, string name)
    {
        Choose(main, category);
        main.Categories.SubcategoryName = name;
        main.AddSubcategoryCommand.Execute(null);
    }

    private static void AddRuleInto(MainViewModel main, string text, string category, string subcategory)
    {
        var form = main.RuleForm;
        form.SelectedKind = ConditionKind.Text;
        form.Text = text;
        form.SelectedCategory = form.Categories.Single(c => c.Name == category);
        form.SelectedSubcategory = form.Subcategories.Single(s => s.Name == subcategory);
        main.AddRuleCommand.Execute(null);
    }

    [Theory]
    [InlineData(CategoryRole.Spending, "výdavky")]
    [InlineData(CategoryRole.Income, "príjmy")]
    [InlineData(CategoryRole.Excluded, "mimo výdavkov")]
    public async Task A_category_of_each_role_is_added_before_the_uncategorized_ones_with_its_Ostatne_and_a_confirmation(CategoryRole role, string tag)
    {
        var main = await Start();

        AddCategory(main, "Šport", role);

        var rows = main.Categories.Rows;
        Assert.Equal(17, rows.Count);
        Assert.Equal("Šport", rows[14].Name);
        Assert.Equal(tag, rows[14].RoleTag);
        Assert.True(rows[14].IsOwn);
        Assert.Equal(["Ostatné"], rows[14].Subcategories.Select(s => s.Name));
        Assert.Equal("Kategória „Šport“ je pridaná.", main.Categories.Message);
        Assert.Null(main.Categories.Error);
        Assert.Null(main.Categories.NameError);
    }

    [Fact]
    public async Task After_adding_a_category_the_form_closes_the_role_stays_and_the_new_category_is_chosen()
    {
        var main = await Start();

        AddCategory(main, "Šport", CategoryRole.Excluded);

        Assert.False(main.Categories.IsAddingCategory);
        Assert.True(main.Categories.ShowsDetails);
        Assert.Equal("", main.Categories.NewName);
        Assert.Equal(CategoryRole.Excluded, main.Categories.SelectedRole.Role);
        Assert.Equal("Šport", main.Categories.SelectedRow?.Name);
        Assert.Equal("Nepočíta sa do výdavkov", main.Categories.SelectedRow?.RoleText);
        Assert.Equal("Kategória „Šport“ je pridaná.", main.Categories.Message);
    }

    [Fact]
    public async Task The_role_is_spending_at_first()
    {
        var main = await Start();

        Assert.Equal(CategoryRole.Spending, main.Categories.SelectedRole.Role);
        Assert.Equal(["Výdavky", "Príjmy", "Mimo výdavkov"], main.Categories.Roles.Select(r => r.Label));
    }

    [Fact]
    public async Task Spaces_around_a_name_are_removed_and_spaces_inside_are_kept()
    {
        var main = await Start();

        AddCategory(main, "  Šport a voľný čas  ");

        Assert.Equal("Šport a voľný čas", main.Categories.Rows[14].Name);
        Assert.Equal("Kategória „Šport a voľný čas“ je pridaná.", main.Categories.Message);
    }

    [Fact]
    public async Task A_name_is_cut_at_forty_characters()
    {
        var main = await Start();

        main.Categories.NewName = new string('x', 60);
        main.Categories.SubcategoryName = new string('y', 60);

        Assert.Equal(40, main.Categories.NewName.Length);
        Assert.Equal(40, main.Categories.SubcategoryName.Length);
    }

    [Theory]
    [InlineData("", "Zadajte názov.")]
    [InlineData("   ", "Zadajte názov.")]
    [InlineData("Bývanie", "Taký názov už existuje.")]
    [InlineData("bývanie", "Taký názov už existuje.")]
    [InlineData("Byvanie", "Taký názov už existuje.")]
    [InlineData("Nezaradené výdavky", "Taký názov už existuje.")]
    [InlineData("Šport", "Taký názov už existuje.")]
    [InlineData("Sport", "Taký názov už existuje.")]
    public async Task A_category_name_that_cannot_be_used_is_refused_with_the_reason_and_stays_typed(string name, string reason)
    {
        var main = await Start();
        AddCategory(main, "Šport");

        AddCategory(main, name);

        Assert.Equal(reason, main.Categories.NameError);
        Assert.Equal(name, main.Categories.NewName);
        Assert.Equal(17, main.Categories.Rows.Count);
        Assert.Null(main.Categories.Message);
    }

    [Fact]
    public async Task A_refusal_goes_when_the_name_changes()
    {
        var main = await Start();
        AddCategory(main, "");
        Assert.NotNull(main.Categories.NameError);

        main.Categories.NewName = "Š";

        Assert.Null(main.Categories.NameError);
    }

    [Fact]
    public async Task At_start_the_first_category_is_chosen_and_its_details_show()
    {
        var main = await Start();

        Assert.Equal("Bývanie", main.Categories.SelectedRow?.Name);
        Assert.Equal("Počíta sa ako výdavky", main.Categories.SelectedRow?.RoleText);
        Assert.True(main.Categories.ShowsDetails);
        Assert.False(main.Categories.IsAddingCategory);
    }

    [Fact]
    public async Task Choosing_another_category_empties_the_sub_category_field_its_reason_and_the_message()
    {
        var main = await Start();
        AddSubcategory(main, "Stravovanie", "Potraviny");
        Assert.NotNull(main.Categories.SubcategoryError);

        Choose(main, "Bývanie");

        Assert.Equal("", main.Categories.SubcategoryName);
        Assert.Null(main.Categories.SubcategoryError);
        Assert.Null(main.Categories.Message);
    }

    [Fact]
    public async Task Nova_kategoria_shows_the_form_in_place_of_the_details_and_Zrusit_goes_back_adding_nothing()
    {
        var main = await Start();

        main.Categories.StartNewCategoryCommand.Execute(null);
        Assert.True(main.Categories.IsAddingCategory);
        Assert.False(main.Categories.ShowsDetails);
        main.Categories.NewName = "Šport";

        main.Categories.CancelNewCategoryCommand.Execute(null);

        Assert.False(main.Categories.IsAddingCategory);
        Assert.True(main.Categories.ShowsDetails);
        Assert.Equal("Bývanie", main.Categories.SelectedRow?.Name);
        Assert.Equal(16, main.Categories.Rows.Count);
        Assert.Equal("", main.Categories.NewName);
    }

    [Fact]
    public async Task Choosing_a_category_while_the_form_shows_closes_the_form()
    {
        var main = await Start();
        main.Categories.StartNewCategoryCommand.Execute(null);

        Choose(main, "Stravovanie");

        Assert.False(main.Categories.IsAddingCategory);
        Assert.True(main.Categories.ShowsDetails);
    }

    [Fact]
    public async Task The_uncategorized_categories_take_no_sub_category()
    {
        var main = await Start();

        AddSubcategory(main, "Nezaradené výdavky", "Niečo");

        Assert.False(main.Categories.SelectedRow!.CanAddSubcategory);
        Assert.Empty(main.Categories.SelectedRow.Subcategories);
        Assert.Null(main.Categories.Message);
    }

    [Fact]
    public async Task The_categorys_Ostatne_is_marked_as_the_one_for_everything_else()
    {
        var main = await Start();

        var food = main.Categories.Rows.Single(r => r.Name == "Stravovanie");

        Assert.Equal([false, false, true], food.Subcategories.Select(s => s.IsOther));
        Assert.Equal("pre všetko, čo nepatrí inam", Texts.OtherHint);
    }

    [Fact]
    public async Task A_sub_category_is_added_to_a_built_in_category_before_Ostatne_with_a_confirmation_and_the_field_empties()
    {
        var main = await Start();

        AddSubcategory(main, "Stravovanie", "Obedy v práci");

        var food = main.Categories.Rows.Single(r => r.Name == "Stravovanie");
        Assert.Equal(["Potraviny", "Reštaurácie a kaviarne", "Obedy v práci", "Ostatné"], food.Subcategories.Select(s => s.Name));
        Assert.Equal([false, false, true, false], food.Subcategories.Select(s => s.IsOwn));
        Assert.Equal("Podkategória „Obedy v práci“ je pridaná do kategórie „Stravovanie“.", main.Categories.Message);
        Assert.Same(food, main.Categories.SelectedRow);
        Assert.Equal("", main.Categories.SubcategoryName);
    }

    [Fact]
    public async Task A_sub_category_is_added_to_a_category_of_the_users_own()
    {
        var main = await Start();
        AddCategory(main, "Šport");

        AddSubcategory(main, "Šport", "Vybavenie");

        Assert.Equal(["Vybavenie", "Ostatné"], main.Categories.Rows.Single(r => r.Name == "Šport").Subcategories.Select(s => s.Name));
    }

    [Theory]
    [InlineData("", "Zadajte názov.")]
    [InlineData("Ostatné", "Taký názov už existuje.")]
    [InlineData("ostatne", "Taký názov už existuje.")]
    [InlineData("Potraviny", "Taký názov už existuje.")]
    public async Task A_sub_category_name_that_cannot_be_used_is_refused_and_stays_typed(string name, string reason)
    {
        var main = await Start();

        AddSubcategory(main, "Stravovanie", name);

        Assert.Equal(reason, main.Categories.SubcategoryError);
        Assert.Equal("Stravovanie", main.Categories.SelectedRow?.Name);
        Assert.Equal(name, main.Categories.SubcategoryName);
        Assert.Equal(3, main.Categories.Rows.Single(r => r.Name == "Stravovanie").Subcategories.Count);
        Assert.Null(main.Categories.Message);
    }

    [Fact]
    public async Task A_sub_category_may_share_its_name_with_another_categorys_sub_category_or_with_its_own_category()
    {
        var main = await Start();
        AddCategory(main, "Šport");

        AddSubcategory(main, "Bývanie", "Potraviny");
        AddSubcategory(main, "Šport", "Šport");

        Assert.Contains(main.Categories.Rows.Single(r => r.Name == "Bývanie").Subcategories, s => s.Name == "Potraviny" && s.IsOwn);
        Assert.Contains(main.Categories.Rows.Single(r => r.Name == "Šport").Subcategories, s => s.Name == "Šport");
    }

    [Fact]
    public async Task The_refusal_of_a_sub_category_goes_when_its_name_changes()
    {
        var main = await Start();
        AddSubcategory(main, "Stravovanie", "");

        main.Categories.SubcategoryName = "O";

        Assert.Null(main.Categories.SubcategoryError);
    }

    [Fact]
    public async Task The_new_category_and_the_new_sub_category_keep_their_own_text()
    {
        var main = await Start();
        main.Categories.SubcategoryName = "Niečo";
        main.Categories.StartNewCategoryCommand.Execute(null);
        main.Categories.NewName = "Šport";

        Assert.Equal("Šport", main.Categories.NewName);
        Assert.Equal("Niečo", main.Categories.SubcategoryName);
    }

    [Fact]
    public async Task A_new_category_and_sub_category_are_choices_in_the_rule_form_at_once_with_Ostatne_last()
    {
        var main = await Start();

        AddCategory(main, "Šport");
        AddSubcategory(main, "Šport", "Vybavenie");
        AddSubcategory(main, "Stravovanie", "Obedy v práci");

        var form = main.RuleForm;
        Assert.Equal("Šport", form.Categories[14].Name);
        form.SelectedCategory = form.Categories[14];
        Assert.Equal(["Vybavenie", "Ostatné"], form.Subcategories.Select(s => s.Name));
        form.SelectedCategory = form.Categories.Single(c => c.Name == "Stravovanie");
        Assert.Equal(["Potraviny", "Reštaurácie a kaviarne", "Obedy v práci", "Ostatné"], form.Subcategories.Select(s => s.Name));
    }

    [Fact]
    public async Task A_rule_being_edited_keeps_its_choices_when_a_category_is_added()
    {
        var main = await Start();
        AddRuleInto(main, "minimarket", "Stravovanie", "Potraviny");
        main.EditRuleCommand.Execute(main.Rules[0]);

        AddCategory(main, "Šport");

        Assert.True(main.RuleForm.IsEditing);
        Assert.Equal("minimarket", main.RuleForm.Text);
        Assert.Equal("Stravovanie", main.RuleForm.SelectedCategory?.Name);
        Assert.Equal("Potraviny", main.RuleForm.SelectedSubcategory?.Name);
        Assert.Contains(main.RuleForm.Categories, c => c.Name == "Šport");
    }

    [Fact]
    public async Task A_half_typed_rule_keeps_its_text_and_its_chosen_category_and_sub_category_when_one_is_added()
    {
        var main = await Start();
        var form = main.RuleForm;
        form.Text = "minimarket";
        form.SelectedCategory = form.Categories.Single(c => c.Name == "Stravovanie");
        form.SelectedSubcategory = form.Subcategories.Single(s => s.Name == "Potraviny");

        AddCategory(main, "Šport");
        AddSubcategory(main, "Stravovanie", "Obedy v práci");

        Assert.Equal("minimarket", form.Text);
        Assert.Equal("Stravovanie", form.SelectedCategory?.Name);
        Assert.Equal("Potraviny", form.SelectedSubcategory?.Name);
    }

    [Fact]
    public async Task Adding_a_category_or_sub_category_does_not_change_how_the_statement_is_categorized()
    {
        var main = await Start();
        var before = main.Statement.Categorized!.Transactions.Select(t => t.Placement).ToList();
        var slices = main.Statement.Slices.Select(s => s.Name).ToList();

        AddCategory(main, "Šport");
        AddSubcategory(main, "Šport", "Vybavenie");
        AddSubcategory(main, "Stravovanie", "Obedy v práci");

        Assert.Equal(before, main.Statement.Categorized!.Transactions.Select(t => t.Placement));
        Assert.Equal(slices, main.Statement.Slices.Select(s => s.Name));
        Assert.DoesNotContain(main.Statement.Categories, c => c.Name == "Šport");
    }

    [Fact]
    public async Task A_rule_into_a_new_sub_category_catches_transactions_and_fills_the_category()
    {
        var main = await Start();
        AddCategory(main, "Šport");
        AddSubcategory(main, "Šport", "Vybavenie");

        AddRuleInto(main, "minimarket", "Šport", "Vybavenie");

        Assert.Equal("Pravidlo zaradilo 3 transakcie.", main.RuleForm.Message);
        Assert.Equal("Šport › Vybavenie", main.Rules[0].Target);
        Assert.Equal(3, main.Statement.Categories.Single(c => c.Name == "Šport").Count);
    }

    [Fact]
    public async Task A_new_spending_category_that_a_rule_fills_gets_a_slice_and_built_in_colours_stay()
    {
        var main = await Start();
        var colours = main.Statement.Slices.ToDictionary(s => s.Name, s => s.Color);

        AddCategory(main, "Šport");
        AddRuleInto(main, "minimarket", "Šport", "Ostatné");

        var slices = main.Statement.Slices;
        Assert.Contains(slices, s => s.Name == "Šport");
        foreach (var (name, colour) in colours.Where(c => slices.Any(s => s.Name == c.Key)))
            Assert.Equal(colour, slices.Single(s => s.Name == name).Color);
    }

    [Fact]
    public async Task A_new_excluded_category_that_a_rule_fills_is_out_of_the_pie_and_the_spending_total_but_still_listed()
    {
        var main = await Start();
        var total = main.Statement.SpendingTotal;
        AddCategory(main, "Dane", CategoryRole.Excluded);

        AddRuleInto(main, "minimarket", "Dane", "Ostatné");

        Assert.DoesNotContain(main.Statement.Slices, s => s.Name == "Dane");
        Assert.NotEqual(total, main.Statement.SpendingTotal);
        Assert.Contains(main.Statement.Categories, c => c.Name == "Dane");
    }

    [Fact]
    public async Task A_new_income_category_that_a_rule_fills_is_not_spending()
    {
        var main = await Start();
        AddCategory(main, "Plat navyše", CategoryRole.Income);

        AddRuleInto(main, "mzda", "Plat navyše", "Ostatné");

        Assert.DoesNotContain(main.Statement.Slices, s => s.Name == "Plat navyše");
        Assert.Equal(1, main.Statement.Categories.Single(c => c.Name == "Plat navyše").Count);
        Assert.True(main.Statement.Categorized!.IncomeTotal > 0);
    }

    [Fact]
    public async Task The_new_category_the_sub_category_and_the_rule_are_back_after_a_restart()
    {
        var main = await Start();
        AddCategory(main, "Šport", CategoryRole.Excluded);
        AddSubcategory(main, "Šport", "Vybavenie");
        AddRuleInto(main, "minimarket", "Šport", "Vybavenie");
        var placements = main.Statement.Categorized!.Transactions.Select(t => t.Placement).ToList();

        var again = await Start();

        var sport = again.Categories.Rows.Single(r => r.Name == "Šport");
        Assert.Equal("mimo výdavkov", sport.RoleTag);
        Assert.Equal(["Vybavenie", "Ostatné"], sport.Subcategories.Select(s => s.Name));
        Assert.Equal("Šport › Vybavenie", again.Rules[0].Target);
        Assert.Equal(placements, again.Statement.Categorized!.Transactions.Select(t => t.Placement));
    }

    [Fact]
    public async Task When_saving_fails_the_card_says_so_and_the_category_still_works()
    {
        File.WriteAllText(_folder, "a file where the folder should be");
        var main = await Start();

        AddCategory(main, "Šport");
        Assert.Null(main.Categories.Message);
        Assert.Equal(Texts.CategorySaveFailed, main.Categories.Error);
        Assert.Contains(main.RuleForm.Categories, c => c.Name == "Šport");

        AddSubcategory(main, "Šport", "Vybavenie");
        Assert.Equal(Texts.SubcategorySaveFailed, main.Categories.Error);
        Assert.Contains(main.Categories.Rows.Single(r => r.Name == "Šport").Subcategories, s => s.Name == "Vybavenie");
    }

    [Fact]
    public async Task When_changes_are_not_saved_this_session_the_card_says_they_last_until_closing()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(RulesFile, "{ \"version\": 2 }");
        var main = await Start();
        Assert.True(main.RulesNotSaving);

        AddCategory(main, "Šport");
        Assert.Equal("Kategória platí len do zatvorenia DocuSpend.", main.Categories.Error);
        Assert.Null(main.Categories.Message);

        AddSubcategory(main, "Šport", "Vybavenie");
        Assert.Equal("Podkategória platí len do zatvorenia DocuSpend.", main.Categories.Error);
        Assert.Contains(main.Categories.Rows.Single(r => r.Name == "Šport").Subcategories, s => s.Name == "Vybavenie");
    }

    [Fact]
    public async Task A_later_successful_save_saves_a_category_whose_save_failed()
    {
        File.WriteAllText(_folder, "blocked");
        var main = await Start();
        AddCategory(main, "Šport");
        File.Delete(_folder);

        AddCategory(main, "Dane", CategoryRole.Excluded);

        var again = new MainViewModel(new RuleStore(RulesFile));
        Assert.Contains(again.Categories.Rows, r => r.Name == "Šport");
        Assert.Contains(again.Categories.Rows, r => r.Name == "Dane");
    }

    [Fact]
    public async Task The_screen_keeps_its_state_when_another_statement_is_opened()
    {
        var main = await Start();
        AddCategory(main, "Šport");
        Choose(main, "Stravovanie");
        main.Categories.SubcategoryName = "Obedy";

        await main.OpenFileAsync(Samples.Path("02-busy-month"));

        Assert.Contains(main.Categories.Rows, r => r.Name == "Šport");
        Assert.Equal("Stravovanie", main.Categories.SelectedRow?.Name);
        Assert.Equal("Obedy", main.Categories.SubcategoryName);
    }

    [Fact]
    public async Task The_form_for_a_new_category_keeps_its_text_when_another_statement_is_opened()
    {
        var main = await Start();
        main.Categories.StartNewCategoryCommand.Execute(null);
        main.Categories.NewName = "Dane";

        await main.OpenFileAsync(Samples.Path("02-busy-month"));

        Assert.True(main.Categories.IsAddingCategory);
        Assert.Equal("Dane", main.Categories.NewName);
    }

    [Fact]
    public async Task Choosing_a_category_and_switching_screens_keeps_the_choice()
    {
        var main = await Start();
        Choose(main, "Auto-moto");

        main.SelectedScreen = main.Screens.Single(s => s.Screen == AppScreen.Categories);
        Assert.True(main.ShowsCategories);
        Assert.False(main.ShowsRules);
        main.SelectedScreen = main.Screens[0];

        Assert.Equal("Auto-moto", main.Categories.SelectedRow?.Name);
    }
}
