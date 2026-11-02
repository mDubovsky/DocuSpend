using DocuSpend.Categorization.Model;

namespace DocuSpend.Presentation;

/// <summary>
/// The order the screens show categories and sub-categories in (work/025-own-categories/spec.md). The categorization
/// itself appends what the user adds after everything else, so this sorts for display only and nothing saved changes:
/// built-in categories, then the user's own in the order added, then the two Uncategorized buckets; in a category,
/// "Ostatné" is always last.
/// </summary>
public static class CatalogOrder
{
    public static IReadOnlyList<Category> Categories(CategoryCatalog catalog) =>
    [
        .. catalog.Categories.Where(c => c.IsBuiltIn && !c.IsUncategorized),
        .. catalog.Categories.Where(c => !c.IsBuiltIn),
        .. catalog.Categories.Where(c => c.IsUncategorized),
    ];

    public static IReadOnlyList<Subcategory> Subcategories(Category category) =>
    [
        .. category.Subcategories.Where(s => !IsOther(category, s)),
        .. category.Subcategories.Where(s => IsOther(category, s)),
    ];

    /// <summary>The sub-categories the user added have generated identifiers (decision 0004); an automatic "Ostatné" isn't one.</summary>
    public static bool IsOwn(Category category, Subcategory subcategory) =>
        !IsOther(category, subcategory) && subcategory.Id.Value.StartsWith("user-", StringComparison.Ordinal);

    /// <summary>The category's "Ostatné", made with it, for whatever fits no other sub-category.</summary>
    public static bool IsOther(Category category, Subcategory subcategory) => subcategory.Id.Value == $"{category.Id}.other";
}
