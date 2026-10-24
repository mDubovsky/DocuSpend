using DocuSpend.Categorization.Model;

namespace DocuSpend.Presentation.Look;

/// <summary>
/// The spending chart's colours. Mid-tone colours, each at least 3 to 1 against both the light and the dark card,
/// so a category keeps its colour in both looks. A category's colour comes from its place in the catalog, so it is
/// the same in every statement, whatever other categories a month has.
/// </summary>
public static class SliceColors
{
    public static IReadOnlyList<string> All { get; } =
    [
        "#5B6CF0", "#E0607E", "#2E9E8F", "#C9772A", "#8A63D2", "#3F8FD6", "#C2559D",
        "#5F9E3C", "#C76B4A", "#B8860B", "#2F9AB0", "#A0679C", "#6E8B3D", "#D45D5D",
    ];

    /// <summary>Uncategorized spending is a neutral grey-blue, so it doesn't look like a category of its own.</summary>
    public const string Uncategorized = "#7D8399";

    public static string For(CategoryCatalog catalog, CategoryId category)
    {
        if (catalog.FindCategory(category) is { IsUncategorized: true })
            return Uncategorized;
        var index = catalog.Categories
            .Where(c => c.Role == CategoryRole.Spending && !c.IsUncategorized)
            .Select(c => c.Id)
            .ToList()
            .IndexOf(category);
        return All[Math.Max(index, 0) % All.Count];
    }
}
