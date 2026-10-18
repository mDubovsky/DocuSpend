using DocuSpend.Categorization.BuiltIn;
using DocuSpend.Categorization.Model;

namespace DocuSpend.Categorization.Tests;

/// <summary>The built-in categories (spec: Built-in categories).</summary>
public class BuiltInCatalogTests
{
    private static readonly CategoryCatalog Catalog = BuiltInCatalog.Create();

    /// <summary>The spec's table: category, role and sub-categories besides Ostatné.</summary>
    private static readonly (string Name, CategoryRole Role, string[] Subs)[] Expected =
    [
        ("Bývanie", CategoryRole.Spending, ["Nájom", "Vybavenie a doplnky", "Úvery na bývanie"]),
        ("Nákupy online", CategoryRole.Spending, ["Webové služby"]),
        ("Stravovanie", CategoryRole.Spending, ["Potraviny", "Reštaurácie a kaviarne"]),
        ("Auto-moto", CategoryRole.Spending, ["Pohonné hmoty", "Servis", "Nákup vozidla"]),
        ("Ostatné výdavky", CategoryRole.Spending, ["Splátky a financovanie", "Platby poistného"]),
        ("Elektronika, komunikácia a médiá", CategoryRole.Spending, ["Účty za telefón", "Elektronika", "TV a rádio"]),
        ("Zdravie a osobná starostlivosť", CategoryRole.Spending, ["Drogéria", "Lekári a nemocnice"]),
        ("Doprava", CategoryRole.Spending, ["Taxi", "Verejná doprava"]),
        ("Vzdelanie", CategoryRole.Spending, ["Kurzy a doučovanie", "Škola a škôlka", "Školské potreby"]),
        ("Poplatky a úroky", CategoryRole.Spending, ["Poplatky", "Úroky"]),
        ("Zábava a oddych", CategoryRole.Spending, ["Hry a hračky"]),
        ("Príjmy", CategoryRole.Income, ["Mzda", "Náhrady výdavkov", "Dávky a prídavky"]),
        ("Prevody medzi vlastnými účtami", CategoryRole.Excluded, []),
        ("Investície", CategoryRole.Excluded, []),
    ];

    [Fact]
    public void The_categories_are_those_of_the_spec_in_order_followed_by_the_two_buckets()
    {
        var names = Catalog.Categories.Select(c => c.Name).ToList();

        Assert.Equal(Expected.Select(e => e.Name).Concat(["Nezaradené výdavky", "Nezaradené príjmy"]), names);
    }

    [Fact]
    public void Each_category_has_the_roles_and_sub_categories_of_the_spec()
    {
        foreach (var (name, role, subs) in Expected)
        {
            var category = Catalog.Categories.Single(c => c.Name == name);

            Assert.Equal(role, category.Role);
            Assert.Equal(subs.Concat(["Ostatné"]), category.Subcategories.Select(s => s.Name));
        }
    }

    [Fact]
    public void Every_category_except_the_buckets_has_an_Ostatne_sub_category() =>
        Assert.All(Catalog.Categories.Where(c => !c.IsUncategorized), c =>
            Assert.Contains(c.Subcategories, s => s.Name == "Ostatné"));

    [Fact]
    public void The_two_buckets_have_no_sub_categories_and_the_roles_of_their_direction()
    {
        var outgoing = Catalog.UncategorizedFor(incoming: false);
        var incoming = Catalog.UncategorizedFor(incoming: true);

        Assert.Equal("Nezaradené výdavky", outgoing.Name);
        Assert.Equal(CategoryRole.Spending, outgoing.Role);
        Assert.Empty(outgoing.Subcategories);
        Assert.True(outgoing.IsUncategorized);
        Assert.Equal("Nezaradené príjmy", incoming.Name);
        Assert.Equal(CategoryRole.Income, incoming.Role);
        Assert.Empty(incoming.Subcategories);
    }

    [Fact]
    public void Built_in_categories_are_marked_built_in() =>
        Assert.All(Catalog.Categories, c => Assert.True(c.IsBuiltIn));

    [Fact]
    public void Identifiers_are_unique()
    {
        var categoryIds = Catalog.Categories.Select(c => c.Id).ToList();
        var subIds = Catalog.Categories.SelectMany(c => c.Subcategories).Select(s => s.Id).ToList();

        Assert.Equal(categoryIds.Count, categoryIds.Distinct().Count());
        Assert.Equal(subIds.Count, subIds.Distinct().Count());
    }

    [Theory]
    [InlineData("housing", "Bývanie")]
    [InlineData("income", "Príjmy")]
    [InlineData("own-transfers", "Prevody medzi vlastnými účtami")]
    [InlineData("investments", "Investície")]
    [InlineData("uncategorized-out", "Nezaradené výdavky")]
    [InlineData("uncategorized-in", "Nezaradené príjmy")]
    public void Category_identifiers_are_stable_english_keys_not_names(string id, string name) =>
        Assert.Equal(name, Catalog.FindCategory(new CategoryId(id))!.Name);

    [Theory]
    [InlineData("housing.rent", "Nájom")]
    [InlineData("housing.mortgage", "Úvery na bývanie")]
    [InlineData("housing.other", "Ostatné")]
    [InlineData("online-shopping.web-services", "Webové služby")]
    [InlineData("food.groceries", "Potraviny")]
    [InlineData("fees.fees", "Poplatky")]
    [InlineData("income.salary", "Mzda")]
    [InlineData("own-transfers.other", "Ostatné")]
    public void Sub_category_identifiers_are_stable_english_keys_not_names(string id, string name) =>
        Assert.Equal(name, Catalog.FindSubcategory(new SubcategoryId(id))!.Value.Subcategory.Name);

    [Fact]
    public void A_sub_category_is_found_with_its_category()
    {
        var found = Catalog.FindSubcategory(new SubcategoryId("housing.rent"))!.Value;

        Assert.Equal("Bývanie", found.Category.Name);
        Assert.Equal("Nájom", found.Subcategory.Name);
    }

    [Fact]
    public void Unknown_identifiers_are_not_found()
    {
        Assert.Null(Catalog.FindCategory(new CategoryId("nope")));
        Assert.Null(Catalog.FindSubcategory(new SubcategoryId("housing.nope")));
    }

    [Fact]
    public void A_placement_is_resolved_to_names_and_a_bucket_placement_has_no_sub_category()
    {
        var rent = Catalog.Describe(new Placement(new CategoryId("housing"), new SubcategoryId("housing.rent")));
        var bucket = Catalog.Describe(new Placement(new CategoryId("uncategorized-out"), null));

        Assert.Equal(("Bývanie", "Nájom"), (rent.Category.Name, rent.Subcategory!.Name));
        Assert.Equal("Nezaradené výdavky", bucket.Category.Name);
        Assert.Null(bucket.Subcategory);
    }
}
