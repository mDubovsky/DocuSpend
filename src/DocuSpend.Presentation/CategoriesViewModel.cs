using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuSpend.Categorization.Model;

namespace DocuSpend.Presentation;

/// <summary>A sub-category as the chosen category's details show it.</summary>
/// <param name="IsOwn">The user added it.</param>
/// <param name="IsOther">The category's "Ostatné", for whatever fits no other sub-category.</param>
public sealed record SubcategoryItem(SubcategoryId Id, string Name, bool IsOwn, bool IsOther);

/// <summary>A category in the list on the "Kategórie" screen, with what its details show.</summary>
/// <param name="RoleTag">The role in a word or two: "výdavky", "príjmy", "mimo výdavkov".</param>
/// <param name="RoleText">The role as a sentence, for the details.</param>
/// <param name="IsOwn">The user made it.</param>
/// <param name="CanAddSubcategory">False for the two Uncategorized buckets, which have no sub-categories.</param>
public sealed record CategoryItem(
    CategoryId Id,
    string Name,
    string RoleTag,
    string RoleText,
    bool IsOwn,
    bool CanAddSubcategory,
    IReadOnlyList<SubcategoryItem> Subcategories);

/// <summary>A role as the choice for a new category offers it.</summary>
public sealed record RoleOption(CategoryRole Role, string Label);

/// <summary>
/// The "Kategórie" screen (work/025-own-categories/spec.md): the list of categories, the chosen category's details
/// with a field for a new sub-category, and the form for a new category, shown only when asked for. Adding goes
/// through <see cref="MainViewModel"/>, which saves.
/// </summary>
public sealed partial class CategoriesViewModel : ObservableObject
{
    private bool _refreshing;

    public CategoriesViewModel(CategoryCatalog catalog)
    {
        Rows = Describe(catalog);
        SelectedRole = Roles[0];
        SelectedRow = Rows[0];
    }

    /// <summary>Every category in display order, rebuilt after every change.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<CategoryItem> Rows { get; private set; }

    /// <summary>The category whose details show; the first one at start.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsDetails))]
    public partial CategoryItem? SelectedRow { get; set; }

    /// <summary>The form for a new category shows in place of the details.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsDetails))]
    public partial bool IsAddingCategory { get; private set; }

    public bool ShowsDetails => !IsAddingCategory && SelectedRow is not null;

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

    /// <summary>The new sub-category's name as typed under the chosen category, cut at <see cref="Texts.MaxNameLength"/> characters.</summary>
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

    /// <summary>Shows the catalog as it is now, with the same category chosen.</summary>
    public void Refresh(CategoryCatalog catalog)
    {
        var selected = SelectedRow?.Id;
        _refreshing = true;
        Rows = Describe(catalog);
        SelectedRow = Rows.FirstOrDefault(r => r.Id == selected) ?? Rows[0];
        _refreshing = false;
    }

    /// <summary>"Nová kategória": the form for a new category, empty, in place of the details.</summary>
    [RelayCommand]
    private void StartNewCategory()
    {
        NewName = "";
        NameError = null;
        Message = Error = null;
        IsAddingCategory = true;
    }

    /// <summary>"Zrušiť" in the form: back to the chosen category's details; nothing is added.</summary>
    [RelayCommand]
    private void CancelNewCategory()
    {
        IsAddingCategory = false;
        NewName = "";
        NameError = null;
    }

    /// <summary>A category was added: the form closes and the new category is chosen.</summary>
    internal void Added(CategoryId category)
    {
        IsAddingCategory = false;
        NewName = "";
        NameError = null;
        SelectedRow = Rows.First(r => r.Id == category);
    }

    partial void OnSelectedRowChanged(CategoryItem? oldValue, CategoryItem? newValue)
    {
        // A list replaced on refresh lets go of its selection for a moment; that isn't the user choosing.
        if (_refreshing || oldValue?.Id == newValue?.Id || newValue is null)
            return;
        IsAddingCategory = false;
        SubcategoryName = "";
        SubcategoryError = null;
        Message = Error = null;
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

    private static List<CategoryItem> Describe(CategoryCatalog catalog) =>
        CatalogOrder.Categories(catalog)
            .Select(c => new CategoryItem(
                c.Id,
                c.Name,
                Texts.RoleTag(c.Role),
                Texts.RoleText(c.Role),
                IsOwn: !c.IsBuiltIn,
                CanAddSubcategory: !c.IsUncategorized,
                CatalogOrder.Subcategories(c)
                    .Select(s => new SubcategoryItem(s.Id, s.Name, CatalogOrder.IsOwn(c, s), CatalogOrder.IsOther(c, s)))
                    .ToList()))
            .ToList();
}
