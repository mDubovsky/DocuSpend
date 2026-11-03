using CommunityToolkit.Mvvm.ComponentModel;
using DocuSpend.Categorization.Model;

namespace DocuSpend.Presentation;

/// <summary>
/// The "Zaradiť transakciu" box for one transaction, and the box about fingerprints above it
/// (work/022-categorize-from-list/spec.md). The target and the condition are a rule form of their own, filled in from
/// the transaction, so the box and "Pravidlá" check input the same way.
/// </summary>
public sealed partial class CategorizeViewModel : ObservableObject
{
    public CategorizeViewModel(TransactionRow row, CategoryCatalog catalog)
    {
        Row = row;
        Form = new RuleFormViewModel(catalog);
        Form.Prefill(row.Transaction, row.Target);
        Date = SlovakFormat.Date(row.Transaction.BookingDate);
    }

    /// <summary>The transaction, as its row in the list shows it.</summary>
    public TransactionRow Row { get; }

    public string Date { get; }

    public RuleFormViewModel Form { get; }

    /// <summary>"Len túto transakciu", chosen at first; otherwise "Pravidlom pre všetky podobné transakcie".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ByRule))]
    public partial bool JustThis { get; set; } = true;

    /// <summary>The condition shows only with a rule chosen.</summary>
    public bool ByRule
    {
        get => !JustThis;
        set => JustThis = !value;
    }

    partial void OnJustThisChanged(bool value) => Form.Error = null;

    /// <summary>The box about fingerprints is open above this one.</summary>
    [ObservableProperty]
    public partial bool ShowsPrivacy { get; set; }

    /// <summary>"Túto správu už nezobrazovať", ticked in the box about fingerprints.</summary>
    [ObservableProperty]
    public partial bool HideNoteNextTime { get; set; }
}
