using DocuSpend.Categorization;
using DocuSpend.Categorization.BuiltIn;
using DocuSpend.Categorization.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>The "Kategórie" card on the rules screen (work/025-own-categories/spec.md).</summary>
public sealed class CategoriesTests : IDisposable
{
    private readonly string _folder = Path.Join(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    private string RulesFile => Path.Join(_folder, "rules.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private static CategorizationConfig WithOwn()
    {
        var config = CategorizationConfig.Default;
        config = config.AddCategory("Šport", CategoryRole.Spending).Value!;
        var sport = config.Catalog.Categories.Single(c => c.Name == "Šport");
        config = config.AddSubcategory(sport.Id, "Vybavenie").Value!;
        config = config.AddSubcategory(new CategoryId("food"), "Obedy v práci").Value!;
        config = config.AddCategory("Plat navyše", CategoryRole.Income).Value!;
        config = config.AddCategory("Dane", CategoryRole.Excluded).Value!;
        return config;
    }

    [Fact]
    public void Without_categories_of_the_users_own_the_card_lists_the_built_in_ones_then_the_two_uncategorized()
    {
        var built = BuiltInCatalog.Create().Categories;

        var rows = new CategoriesViewModel(CategorizationConfig.Default.Catalog).Rows;

        Assert.Equal(16, rows.Count);
        Assert.Equal(built.Select(c => c.Name), rows.Select(r => r.Name));
        Assert.Equal(["Nezaradené výdavky", "Nezaradené príjmy"], rows.TakeLast(2).Select(r => r.Name));
        Assert.All(rows, r => Assert.False(r.IsOwn));
    }

    [Fact]
    public void Every_row_has_the_tag_of_its_role()
    {
        var rows = new CategoriesViewModel(CategorizationConfig.Default.Catalog).Rows;

        Assert.Equal("výdavky", rows.Single(r => r.Name == "Bývanie").RoleTag);
        Assert.Equal("príjmy", rows.Single(r => r.Name == "Príjmy").RoleTag);
        Assert.Equal("mimo výdavkov", rows.Single(r => r.Name == "Investície").RoleTag);
        Assert.Equal("výdavky", rows.Single(r => r.Name == "Nezaradené výdavky").RoleTag);
        Assert.Equal("príjmy", rows.Single(r => r.Name == "Nezaradené príjmy").RoleTag);
    }

    [Fact]
    public void Built_in_sub_categories_keep_their_order_with_Ostatne_last_and_none_is_marked_as_own()
    {
        var housing = new CategoriesViewModel(CategorizationConfig.Default.Catalog).Rows.Single(r => r.Name == "Bývanie");

        Assert.Equal(["Nájom", "Vybavenie a doplnky", "Úvery na bývanie", "Ostatné"], housing.Subcategories.Select(s => s.Name));
        Assert.All(housing.Subcategories, s => Assert.False(s.IsOwn));
    }

    [Fact]
    public void The_uncategorized_rows_have_no_sub_categories_and_no_way_to_add_one()
    {
        var rows = new CategoriesViewModel(CategorizationConfig.Default.Catalog).Rows;

        foreach (var row in rows.TakeLast(2))
        {
            Assert.False(row.CanAddSubcategory);
            Assert.Empty(row.Subcategories);
        }
        Assert.All(rows.Take(14), r => Assert.True(r.CanAddSubcategory));
    }

    [Fact]
    public void The_users_own_categories_come_after_the_built_in_ones_and_before_the_uncategorized_ones_in_the_order_added()
    {
        var rows = new CategoriesViewModel(WithOwn().Catalog).Rows;

        Assert.Equal(19, rows.Count);
        Assert.Equal(["Šport", "Plat navyše", "Dane"], rows.Skip(14).Take(3).Select(r => r.Name));
        Assert.Equal(["Nezaradené výdavky", "Nezaradené príjmy"], rows.TakeLast(2).Select(r => r.Name));
        Assert.Equal([true, true, true], rows.Skip(14).Take(3).Select(r => r.IsOwn));
        Assert.Equal(["výdavky", "príjmy", "mimo výdavkov"], rows.Skip(14).Take(3).Select(r => r.RoleTag));
    }

    [Fact]
    public void A_category_of_the_users_own_has_its_Ostatne_and_the_sub_categories_added_before_it()
    {
        var sport = new CategoriesViewModel(WithOwn().Catalog).Rows.Single(r => r.Name == "Šport");

        Assert.Equal(["Vybavenie", "Ostatné"], sport.Subcategories.Select(s => s.Name));
        Assert.Equal([true, false], sport.Subcategories.Select(s => s.IsOwn));
        Assert.True(sport.CanAddSubcategory);
    }

    [Fact]
    public void A_sub_category_of_the_users_own_in_a_built_in_category_goes_before_Ostatne_and_is_marked()
    {
        var food = new CategoriesViewModel(WithOwn().Catalog).Rows.Single(r => r.Name == "Stravovanie");

        Assert.Equal(["Potraviny", "Reštaurácie a kaviarne", "Obedy v práci", "Ostatné"], food.Subcategories.Select(s => s.Name));
        Assert.Equal([false, false, true, false], food.Subcategories.Select(s => s.IsOwn));
        Assert.False(food.IsOwn);
    }

    [Fact]
    public void The_main_view_model_shows_the_saved_categories_after_a_restart()
    {
        Assert.True(new RuleStore(RulesFile).Save(WithOwn()));

        var main = new MainViewModel(new RuleStore(RulesFile));

        Assert.Contains(main.Categories.Rows, r => r.Name == "Šport" && r.IsOwn);
        Assert.Equal(["Potraviny", "Reštaurácie a kaviarne", "Obedy v práci", "Ostatné"],
            main.Categories.Rows.Single(r => r.Name == "Stravovanie").Subcategories.Select(s => s.Name));
    }

    [Fact]
    public void The_rule_form_offers_the_users_categories_after_the_built_in_ones_without_the_uncategorized_ones()
    {
        var form = new RuleFormViewModel(WithOwn().Catalog);

        Assert.Equal(17, form.Categories.Count);
        Assert.Equal(["Šport", "Plat navyše", "Dane"], form.Categories.TakeLast(3).Select(c => c.Name));
        Assert.DoesNotContain(form.Categories, c => c.IsUncategorized);
    }

    [Fact]
    public void The_rule_form_lists_a_categorys_sub_categories_with_Ostatne_last()
    {
        var form = new RuleFormViewModel(WithOwn().Catalog);

        form.SelectedCategory = form.Categories.Single(c => c.Name == "Stravovanie");

        Assert.Equal(["Potraviny", "Reštaurácie a kaviarne", "Obedy v práci", "Ostatné"], form.Subcategories.Select(s => s.Name));
    }

    [Fact]
    public void The_new_texts_are_in_slovak()
    {
        Assert.Equal("Názov", Texts.CategoryName);
        Assert.Equal("Počíta sa ako", Texts.CountsAs);
        Assert.Equal("Pridať kategóriu", Texts.AddCategory);
        Assert.Equal("Pridať podkategóriu", Texts.AddSubcategory);
        Assert.Equal("Pridať", Texts.Add);
        Assert.Equal("vlastná", Texts.Own);
        Assert.Equal("Výdavky", Texts.RoleName(CategoryRole.Spending));
        Assert.Equal("Príjmy", Texts.RoleName(CategoryRole.Income));
        Assert.Equal("Mimo výdavkov", Texts.RoleName(CategoryRole.Excluded));
        Assert.Equal("výdavky", Texts.RoleTag(CategoryRole.Spending));
        Assert.Equal("príjmy", Texts.RoleTag(CategoryRole.Income));
        Assert.Equal("mimo výdavkov", Texts.RoleTag(CategoryRole.Excluded));
        Assert.Equal("Výdavky: platby sa započítajú do výdavkov a do grafu výdavkov. Príjmy: prichádzajúce platby, nikdy sa nepočítajú ako výdavky. Mimo výdavkov: napríklad prevody medzi vlastnými účtami alebo investície; do výdavkov sa nezapočítajú.", Texts.RoleHelp);
        Assert.Equal("Kategória „Šport“ je pridaná.", Texts.CategoryAdded("Šport"));
        Assert.Equal("Podkategória „Obedy v práci“ je pridaná do kategórie „Stravovanie“.", Texts.SubcategoryAdded("Obedy v práci", "Stravovanie"));
        Assert.Equal("Kategória platí len do zatvorenia DocuSpend.", Texts.CategoryOnlyThisSession);
        Assert.Equal("Podkategória platí len do zatvorenia DocuSpend.", Texts.SubcategoryOnlyThisSession);
        Assert.Equal("Kategóriu sa nepodarilo uložiť, preto po zatvorení DocuSpend zmizne. Pri ďalšej zmene sa DocuSpend pokúsi uložiť znova.", Texts.CategorySaveFailed);
        Assert.Equal("Podkategóriu sa nepodarilo uložiť, preto po zatvorení DocuSpend zmizne. Pri ďalšej zmene sa DocuSpend pokúsi uložiť znova.", Texts.SubcategorySaveFailed);
    }
}
