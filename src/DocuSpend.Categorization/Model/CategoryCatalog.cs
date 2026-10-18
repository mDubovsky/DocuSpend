using DocuSpend.Categorization.Rules;

namespace DocuSpend.Categorization.Model;

/// <summary>
/// The categories: built-in plus user-created. Immutable; every change returns a new catalog or a refusal, so a
/// catalog can't be left in a state that breaks the rules of the spec: names unique among siblings, an Ostatné
/// sub-category in every category except the Uncategorized buckets, and built-in categories never removed or changed.
/// </summary>
public sealed class CategoryCatalog
{
    public const string OtherName = "Ostatné";

    private readonly Dictionary<CategoryId, Category> _categories;
    private readonly Dictionary<SubcategoryId, (Category Category, Subcategory Subcategory)> _subcategories;

    public CategoryCatalog(IEnumerable<Category> categories)
    {
        Categories = categories.ToList();
        _categories = Categories.ToDictionary(c => c.Id);
        _subcategories = Categories
            .SelectMany(c => c.Subcategories.Select(s => (Category: c, Subcategory: s)))
            .ToDictionary(x => x.Subcategory.Id);
    }

    public IReadOnlyList<Category> Categories { get; }

    public Category? FindCategory(CategoryId id) => _categories.GetValueOrDefault(id);

    public (Category Category, Subcategory Subcategory)? FindSubcategory(SubcategoryId id) =>
        _subcategories.TryGetValue(id, out var found) ? found : null;

    /// <summary>The bucket for transactions no rule matched: one for money coming in, one for money going out.</summary>
    public Category UncategorizedFor(bool incoming) =>
        Categories.Single(c => c.IsUncategorized && (c.Role == CategoryRole.Income) == incoming);

    /// <summary>The category and sub-category a placement refers to. A bucket placement has no sub-category.</summary>
    public (Category Category, Subcategory? Subcategory) Describe(Placement placement)
    {
        var category = _categories[placement.Category];
        var subcategory = placement.Subcategory is { } id ? _subcategories[id].Subcategory : null;
        return (category, subcategory);
    }

    /// <summary>Adds a user category with the chosen role. It gets an Ostatné sub-category automatically.</summary>
    public Outcome<CategoryCatalog> AddCategory(string name, CategoryRole role)
    {
        if (TextMatching.IsBlank(name))
            return Outcome<CategoryCatalog>.Refused(RefusalKind.EmptyName, "A category needs a name.");
        if (Categories.Any(c => SameName(c.Name, name)))
            return Outcome<CategoryCatalog>.Refused(RefusalKind.DuplicateName, "A category with this name already exists.");

        var id = new CategoryId($"user-{Guid.NewGuid():N}");
        var other = new Subcategory(new SubcategoryId($"{id}.other"), OtherName);
        var category = new Category(id, name.Trim(), role, [other], IsBuiltIn: false);
        return Outcome<CategoryCatalog>.Ok(new CategoryCatalog([.. Categories, category]));
    }

    /// <summary>Adds a sub-category to a category, built-in or the user's own.</summary>
    public Outcome<CategoryCatalog> AddSubcategory(CategoryId categoryId, string name)
    {
        if (FindCategory(categoryId) is not { } category)
            return Outcome<CategoryCatalog>.Refused(RefusalKind.UnknownCategory, "The category doesn't exist.");
        if (category.IsUncategorized)
            return Outcome<CategoryCatalog>.Refused(RefusalKind.NoSubcategoriesAllowed, "The Uncategorized categories have no sub-categories.");
        if (TextMatching.IsBlank(name))
            return Outcome<CategoryCatalog>.Refused(RefusalKind.EmptyName, "A sub-category needs a name.");
        if (category.Subcategories.Any(s => SameName(s.Name, name)))
            return Outcome<CategoryCatalog>.Refused(RefusalKind.DuplicateName, "The category already has a sub-category with this name.");

        var added = new Subcategory(new SubcategoryId($"user-{Guid.NewGuid():N}"), name.Trim());
        var changed = category with { Subcategories = [.. category.Subcategories, added] };
        return Outcome<CategoryCatalog>.Ok(new CategoryCatalog(Categories.Select(c => c.Id == categoryId ? changed : c)));
    }

    private static bool SameName(string a, string b) => TextMatching.Fold(a).Trim() == TextMatching.Fold(b).Trim();
}
