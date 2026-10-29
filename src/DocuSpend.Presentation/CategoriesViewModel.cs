using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuSpend.Categorization.Model;

namespace DocuSpend.Presentation;

/// <summary>A sub-category as the card shows it: a small pill, marked when it is the user's own.</summary>
public sealed record SubcategoryPill(SubcategoryId Id, string Name, bool IsOwn);

/// <summary>A role as the choice for a new category offers it.</summary>
public sealed record RoleOption(CategoryRole Role, string Label);

/// <summary>A category as the "Kategórie" card shows it, with the line for adding a sub-category to it.</summary>
public sealed partial class CategoryCardRow(
    CategoryId id,
    string name,
    string roleTag,
    bool isOwn,
    bool canAddSubcategory,
    IReadOnlyList<SubcategoryPill> subcategories) : ObservableObject
{
    public CategoryId Id { get; } = id;

    public string Name { get; } = name;

    public string RoleTag { get; } = roleTag;

    /// <summary>The user made this category.</summary>
    public bool IsOwn { get; } = isOwn;

    /// <summary>False for the two Uncategorized buckets.</summary>
    public bool CanAddSubcategory { get; } = canAddSubcategory;

    public IReadOnlyList<SubcategoryPill> Subcategories { get; } = subcategories;

    /// <summary>The line under the pills, for typing a new sub-category's name, is open.</summary>
    [ObservableProperty]
    public partial bool IsLineOpen { get; set; }
}

/// <summary>
/// The "Kategórie" card on the rules screen (work/025-own-categories/spec.md): the list of categories, the fields for
/// a new one, and the line for a new sub-category. Adding goes through <see cref="MainViewModel"/>, which saves.
/// </summary>
public sealed partial class CategoriesViewModel : ObservableObject
{
    public CategoriesViewModel(CategoryCatalog catalog)
    {
        Rows = Describe(catalog);
        SelectedRole = Roles[0];
    }

    /// <summary>Every category in display order, rebuilt after every change.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<CategoryCardRow> Rows { get; private set; }

    public IReadOnlyList<RoleOption> Roles { get; } =
    [
        new(CategoryRole.Spending, Texts.RoleName(CategoryRole.Spending)),
        new(CategoryRole.Income, Texts.RoleName(CategoryRole.Income)),
        new(CategoryRole.Excluded, Texts.RoleName(CategoryRole.Excluded)),
    ];

    /// <summary>The new category's name as typed, cut at <see cref="Texts.MaxNameLength"/> characters.</summary>
    [ObservableProperty]
    public partial string NewName { get; set; } = "";

    [ObservableProperty]
    public partial RoleOption SelectedRole { get; set; }

    /// <summary>Why the new category's name was refused, or null; gone when the name changes.</summary>
    [ObservableProperty]
    public partial string? NameError { get; set; }

    /// <summary>The row whose line for a new sub-category is open, or null. Only one is open at a time.</summary>
    [ObservableProperty]
    public partial CategoryCardRow? OpenRow { get; private set; }

    /// <summary>The new sub-category's name as typed in the open line, cut at <see cref="Texts.MaxNameLength"/> characters.</summary>
    [ObservableProperty]
    public partial string SubcategoryName { get; set; } = "";

    /// <summary>Why the sub-category's name was refused, or null; gone when the name changes.</summary>
    [ObservableProperty]
    public partial string? SubcategoryError { get; set; }

    /// <summary>What the last addition did, in Slovak, or null.</summary>
    [ObservableProperty]
    public partial string? Message { get; set; }

    /// <summary>Why the last addition was not saved, in Slovak, or null.</summary>
    [ObservableProperty]
    public partial string? Error { get; set; }

    /// <summary>Counts every opening of the line for a new sub-category, so the window can put the cursor in it.</summary>
    [ObservableProperty]
    public partial int LineOpenings { get; private set; }

    /// <summary>The category added last, so the window can bring its row into view.</summary>
    [ObservableProperty]
    public partial CategoryId? LastAdded { get; private set; }

    /// <summary>Shows the catalog as it is now. The open line stays open on the same category.</summary>
    public void Refresh(CategoryCatalog catalog)
    {
        var open = OpenRow?.Id;
        Rows = Describe(catalog);
        OpenRow = open is { } id ? Rows.FirstOrDefault(r => r.Id == id) : null;
        if (OpenRow is not null)
            OpenRow.IsLineOpen = true;
    }

    /// <summary>"Pridať podkategóriu": opens the line under this category, closing any other and emptying the name.</summary>
    [RelayCommand]
    private void OpenLine(CategoryCardRow? row)
    {
        if (row is null || !row.CanAddSubcategory)
            return;
        CloseLine();
        SubcategoryName = "";
        SubcategoryError = null;
        row.IsLineOpen = true;
        OpenRow = row;
        LineOpenings++;
    }

    /// <summary>"Zrušiť": closes the line and adds nothing.</summary>
    [RelayCommand]
    public void CloseLine()
    {
        if (OpenRow is not null)
            OpenRow.IsLineOpen = false;
        OpenRow = null;
        SubcategoryName = "";
        SubcategoryError = null;
    }

    /// <summary>A category was added: empty the name, keep the role, remember it for the window.</summary>
    internal void Added(CategoryId category)
    {
        NewName = "";
        NameError = null;
        Error = null;
        LastAdded = category;
    }

    partial void OnNewNameChanged(string value)
    {
        if (value.Length > Texts.MaxNameLength)
            NewName = value[..Texts.MaxNameLength];
        NameError = null;
    }

    partial void OnSubcategoryNameChanged(string value)
    {
        if (value.Length > Texts.MaxNameLength)
            SubcategoryName = value[..Texts.MaxNameLength];
        SubcategoryError = null;
    }

    private static List<CategoryCardRow> Describe(CategoryCatalog catalog) =>
        CatalogOrder.Categories(catalog)
            .Select(c => new CategoryCardRow(
                c.Id,
                c.Name,
                Texts.RoleTag(c.Role),
                isOwn: !c.IsBuiltIn,
                canAddSubcategory: !c.IsUncategorized,
                CatalogOrder.Subcategories(c).Select(s => new SubcategoryPill(s.Id, s.Name, CatalogOrder.IsOwn(c, s))).ToList()))
            .ToList();
}
