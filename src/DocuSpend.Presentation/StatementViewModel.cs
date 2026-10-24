using CommunityToolkit.Mvvm.ComponentModel;
using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Results;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation;

/// <summary>A slice of the spending pie: a spending category with spending above zero.</summary>
public sealed record PieSlice(CategoryId Category, string Name, decimal Spending, decimal Share, string Color)
{
    public string AmountText => SlovakFormat.Amount(Spending);
    public string ShareText => SlovakFormat.Share(Share);
}

/// <summary>The balance at the end of a day.</summary>
public sealed record BalancePoint(DateOnly Date, decimal Balance);

/// <summary>A category in the list: every category with transactions this month, whatever its role.</summary>
public sealed record CategoryRow(CategoryId Category, string Name, int Count, string Total);

/// <summary>A transaction in the selected category's list.</summary>
public sealed record TransactionRow(Transaction Transaction, string Date, string Description, string Subcategory, string Amount);

/// <summary>
/// What the statement view shows: one statement, categorized with the current configuration, as plain data the
/// charts and lists draw. It knows nothing about the toolkit or the chart library (plan: screen logic).
/// </summary>
public sealed partial class StatementViewModel : ObservableObject
{
    private Statement? _statement;
    private CategorizedStatement? _categorized;

    public StatementViewModel(CategorizationConfig config) => Config = config;

    /// <summary>The categories and rules. Changing it categorizes the statement again and keeps the selection.</summary>
    [ObservableProperty]
    public partial CategorizationConfig Config { get; set; }

    /// <summary>The category whose transactions are listed, chosen in the pie or the category list.</summary>
    [ObservableProperty]
    public partial CategoryId? SelectedCategory { get; set; }

    /// <summary>The category list's selected row; it follows <see cref="SelectedCategory"/> and sets it.</summary>
    [ObservableProperty]
    public partial CategoryRow? SelectedRow { get; set; }

    [ObservableProperty]
    public partial string Period { get; private set; } = "";

    [ObservableProperty]
    public partial string OpeningBalance { get; private set; } = "";

    [ObservableProperty]
    public partial string ClosingBalance { get; private set; } = "";

    [ObservableProperty]
    public partial string SpendingTotal { get; private set; } = "";

    /// <summary>Largest first.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<PieSlice> Slices { get; private set; } = [];

    [ObservableProperty]
    public partial bool HasSpending { get; private set; }

    /// <summary>From the opening balance at the period's start, by booking date, to the closing balance at its end.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<BalancePoint> BalancePoints { get; private set; } = [];

    /// <summary>In the catalog's order.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<CategoryRow> Categories { get; private set; } = [];

    /// <summary>The selected category's transactions, by booking date.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<TransactionRow> Transactions { get; private set; } = [];

    /// <summary>The categorized statement shown, or null before one is shown.</summary>
    public CategorizedStatement? Categorized => _categorized;

    public void Show(Statement statement)
    {
        _statement = statement;
        SelectedCategory = null;
        Refresh();
    }

    partial void OnConfigChanged(CategorizationConfig value) => Refresh();

    partial void OnSelectedCategoryChanged(CategoryId? value)
    {
        SelectedRow = Categories.FirstOrDefault(c => c.Category == value);
        ListTransactions();
    }

    partial void OnSelectedRowChanged(CategoryRow? value)
    {
        // A list clears its selection when its items are replaced; that isn't the user choosing nothing.
        if (value is not null)
            SelectedCategory = value.Category;
    }

    private void Refresh()
    {
        if (_statement is null)
            return;
        var statement = _statement;
        var categorized = _categorized = Categorizer.Categorize(statement, Config);

        Period = SlovakFormat.Period(statement.PeriodStart, statement.PeriodEnd);
        OpeningBalance = SlovakFormat.Amount(statement.OpeningBalance);
        ClosingBalance = SlovakFormat.Amount(statement.ClosingBalance);
        SpendingTotal = SlovakFormat.Amount(categorized.SpendingTotal);

        var spending = categorized.Categories.Where(c => c.Spending > 0).ToList();
        var total = spending.Sum(c => c.Spending!.Value);
        Slices = spending
            .OrderByDescending(c => c.Spending)
            .Select(c => new PieSlice(c.Category.Id, c.Category.Name, c.Spending!.Value, c.Spending!.Value / total,
                Look.SliceColors.For(Config.Catalog, c.Category.Id)))
            .ToList();
        HasSpending = Slices.Count > 0;

        BalancePoints = Balances(statement);
        Categories = categorized.Categories
            .Where(c => c.Total.Count > 0)
            .Select(c => new CategoryRow(c.Category.Id, c.Category.Name, c.Total.Count, SlovakFormat.Amount(c.Total.Amount)))
            .ToList();

        if (SelectedCategory is { } selected && Categories.All(c => c.Category != selected))
            SelectedCategory = null;
        else
        {
            SelectedRow = Categories.FirstOrDefault(c => c.Category == SelectedCategory);
            ListTransactions();
        }
    }

    private void ListTransactions()
    {
        if (_categorized is not { } categorized || SelectedCategory is not { } selected)
        {
            Transactions = [];
            return;
        }
        var catalog = Config.Catalog;
        Transactions = categorized.Transactions
            .Where(t => t.Placement.Category == selected)
            .OrderBy(t => t.Transaction.BookingDate)
            .Select(t =>
            {
                var (category, subcategory) = catalog.Describe(t.Placement);
                var transaction = t.Transaction;
                return new TransactionRow(
                    transaction,
                    SlovakFormat.Date(transaction.BookingDate),
                    transaction.Description ?? transaction.CounterpartyName ?? Texts.NoDescription,
                    subcategory?.Name ?? category.Name,
                    SlovakFormat.Amount(transaction.Amount));
            })
            .ToList();
    }

    private static List<BalancePoint> Balances(Statement statement)
    {
        var points = new List<BalancePoint> { new(statement.PeriodStart, statement.OpeningBalance) };
        var running = statement.OpeningBalance;
        foreach (var day in statement.Transactions.GroupBy(t => t.BookingDate).OrderBy(g => g.Key))
        {
            running += day.Sum(t => t.Amount);
            points.Add(new BalancePoint(day.Key, running));
        }
        if (points[^1].Date != statement.PeriodEnd)
            points.Add(new BalancePoint(statement.PeriodEnd, running));
        return points;
    }
}
