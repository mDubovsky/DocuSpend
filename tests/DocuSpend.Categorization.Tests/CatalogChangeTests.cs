using DocuSpend.Categorization.BuiltIn;
using DocuSpend.Categorization.Model;

namespace DocuSpend.Categorization.Tests;

/// <summary>Adding categories and sub-categories (spec: User-created categories).</summary>
public class CatalogChangeTests
{
    private static readonly CategoryCatalog Start = BuiltInCatalog.Create();
    private static readonly CategoryId Housing = new("housing");

    [Theory]
    [InlineData(CategoryRole.Spending)]
    [InlineData(CategoryRole.Income)]
    [InlineData(CategoryRole.Excluded)]
    public void A_user_category_takes_the_role_the_user_chose(CategoryRole role)
    {
        var outcome = Start.AddCategory("Deti", role);

        Assert.True(outcome.IsSuccess, outcome.Refusal?.Message);
        var added = outcome.Value!.Categories.Single(c => c.Name == "Deti");
        Assert.Equal(role, added.Role);
        Assert.False(added.IsBuiltIn);
        Assert.False(added.IsUncategorized);
    }

    [Fact]
    public void A_user_category_gets_an_Ostatne_sub_category_and_a_generated_identifier()
    {
        var added = Start.AddCategory("Deti", CategoryRole.Spending).Value!.Categories.Single(c => c.Name == "Deti");

        Assert.Equal(["Ostatné"], added.Subcategories.Select(s => s.Name));
        Assert.StartsWith("user-", added.Id.Value);
        Assert.StartsWith(added.Id.Value, added.Subcategories[0].Id.Value);
    }

    [Fact]
    public void Two_user_categories_never_share_an_identifier()
    {
        var catalog = Start.AddCategory("Deti", CategoryRole.Spending).Value!.AddCategory("Zvieratá", CategoryRole.Spending).Value!;
        var ids = catalog.Categories.Select(c => c.Id.Value)
            .Concat(catalog.Categories.SelectMany(c => c.Subcategories).Select(s => s.Id.Value))
            .ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Adding_does_not_change_the_catalog_it_was_added_to()
    {
        var before = Start.Categories.Count;

        Start.AddCategory("Deti", CategoryRole.Spending);

        Assert.Equal(before, Start.Categories.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_category_needs_a_name(string name) =>
        Assert.Equal(RefusalKind.EmptyName, Start.AddCategory(name, CategoryRole.Spending).Refusal!.Kind);

    [Theory]
    [InlineData("Bývanie")]
    [InlineData("byvanie")]
    [InlineData("  BÝVANIE ")]
    [InlineData("Nezaradené výdavky")]
    public void A_category_name_may_not_repeat_another_ignoring_case_and_diacritics(string name) =>
        Assert.Equal(RefusalKind.DuplicateName, Start.AddCategory(name, CategoryRole.Spending).Refusal!.Kind);

    [Fact]
    public void A_user_category_name_may_not_repeat_an_earlier_user_category()
    {
        var catalog = Start.AddCategory("Deti", CategoryRole.Spending).Value!;

        Assert.Equal(RefusalKind.DuplicateName, catalog.AddCategory("deti", CategoryRole.Excluded).Refusal!.Kind);
    }

    [Fact]
    public void A_sub_category_can_be_added_to_a_built_in_category()
    {
        var outcome = Start.AddSubcategory(Housing, "Záhrada");

        Assert.True(outcome.IsSuccess, outcome.Refusal?.Message);
        var category = outcome.Value!.FindCategory(Housing)!;
        Assert.Contains(category.Subcategories, s => s.Name == "Záhrada");
        Assert.Contains(category.Subcategories, s => s.Name == "Ostatné");
        Assert.StartsWith("user-", category.Subcategories.Single(s => s.Name == "Záhrada").Id.Value);
    }

    [Fact]
    public void A_sub_category_can_be_added_to_a_user_category()
    {
        var withUser = Start.AddCategory("Deti", CategoryRole.Spending).Value!;
        var id = withUser.Categories.Single(c => c.Name == "Deti").Id;

        var outcome = withUser.AddSubcategory(id, "Krúžky");

        Assert.True(outcome.IsSuccess, outcome.Refusal?.Message);
        Assert.Equal(["Ostatné", "Krúžky"], outcome.Value!.FindCategory(id)!.Subcategories.Select(s => s.Name));
    }

    [Fact]
    public void The_same_sub_category_name_is_fine_in_a_different_category() =>
        Assert.True(Start.AddSubcategory(new CategoryId("food"), "Nájom").IsSuccess);

    [Theory]
    [InlineData("Nájom")]
    [InlineData("najom")]
    [InlineData("OSTATNE")]
    [InlineData("ostatné")]
    public void A_sub_category_name_may_not_repeat_a_sibling_ignoring_case_and_diacritics(string name) =>
        Assert.Equal(RefusalKind.DuplicateName, Start.AddSubcategory(Housing, name).Refusal!.Kind);

    [Fact]
    public void A_sub_category_needs_a_name() =>
        Assert.Equal(RefusalKind.EmptyName, Start.AddSubcategory(Housing, " ").Refusal!.Kind);

    [Fact]
    public void A_sub_category_needs_an_existing_category() =>
        Assert.Equal(RefusalKind.UnknownCategory, Start.AddSubcategory(new CategoryId("nope"), "X").Refusal!.Kind);

    [Theory]
    [InlineData("uncategorized-out")]
    [InlineData("uncategorized-in")]
    public void The_uncategorized_buckets_take_no_sub_categories(string bucket) =>
        Assert.Equal(RefusalKind.NoSubcategoriesAllowed, Start.AddSubcategory(new CategoryId(bucket), "X").Refusal!.Kind);

    [Fact]
    public void Built_in_categories_cannot_be_removed_or_changed_through_the_catalog()
    {
        var catalog = Start.AddCategory("Deti", CategoryRole.Spending).Value!;

        Assert.All(Start.Categories, builtIn => Assert.Contains(catalog.Categories, c => c == builtIn));
    }
}
