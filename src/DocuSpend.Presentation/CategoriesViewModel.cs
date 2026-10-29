using CommunityToolkit.Mvvm.ComponentModel;
using DocuSpend.Categorization.Model;

namespace DocuSpend.Presentation;

/// <summary>A sub-category as the card shows it: a small pill, marked when it is the user's own.</summary>
public sealed record SubcategoryPill(SubcategoryId Id, string Name, bool IsOwn);

/// <summary>A category as the "Kategórie" card shows it.</summary>
public sealed record CategoryCardRow(
    CategoryId Id,
    string Name,
    string RoleTag,
    bool IsOwn,
    bool CanAddSubcategory,
    IReadOnlyList<SubcategoryPill> Subcategories);

/// <summary>The "Kategórie" card on the rules screen (work/025-own-categories/spec.md).</summary>
public sealed partial class CategoriesViewModel : ObservableObject
{
    public CategoriesViewModel(CategoryCatalog catalog) => Rows = Describe(catalog);

    /// <summary>Every category in display order, rebuilt after every change.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<CategoryCardRow> Rows { get; private set; }

    /// <summary>Shows the catalog as it is now.</summary>
    public void Refresh(CategoryCatalog catalog) => Rows = Describe(catalog);

    private static List<CategoryCardRow> Describe(CategoryCatalog catalog) =>
        CatalogOrder.Categories(catalog)
            .Select(c => new CategoryCardRow(
                c.Id,
                c.Name,
                Texts.RoleTag(c.Role),
                IsOwn: !c.IsBuiltIn,
                CanAddSubcategory: !c.IsUncategorized,
                CatalogOrder.Subcategories(c).Select(s => new SubcategoryPill(s.Id, s.Name, CatalogOrder.IsOwn(c, s))).ToList()))
            .ToList();
}
