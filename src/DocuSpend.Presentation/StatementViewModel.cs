using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Results;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation;

/// <summary>A slice of the spending pie: a spending category with spending above zero.</summary>
public sealed record PieSlice(CategoryId Category, string Name, decimal Spending, decimal Share, string Color)
{
    public string AmountText => SlovakFormat.Amount(Spending);
    public string ShareText => SlovakFormat.Share(Share);
}

/// <summary>The balance at the end of a day, with the texts the balance chart's hover card shows.</summary>
public sealed record BalancePoint(DateOnly Date, decimal Balance)
{
    public string DateText => SlovakFormat.Date(Date);
    public string BalanceText => SlovakFormat.Amount(Balance);
}

/// <summary>A category in the list: every category with transactions this month, whatever its role.</summary>
public sealed record CategoryRow(CategoryId Category, string Name, int Count, decimal Amount, string Total)
{
    /// <summary>More money came in than went out, shown in the positive colour (spec: numbers first).</summary>
    public bool IsIncoming => Amount > 0;
}

/// <summary>A transaction in the selected category's list.</summary>
/// <param name="Index">Its place in the statement, which its fingerprint depends on.</param>
/// <param name="Target">Its sub-category, or null in an Uncategorized bucket.</param>
/// <param name="ByHand">The user chose its sub-category for it alone; the row shows "ručne".</param>
/// <param name="Fingerprint">How its hand choice is found; null before the first hand choice made a key.</param>
public sealed record TransactionRow(
    Transaction Transaction, string Date, string Description, string Subcategory, string Amount,
    int Index = 0, SubcategoryId? Target = null, bool ByHand = false, Fingerprint? Fingerprint = null)
{
    /// <summary>Money coming in, shown in the positive colour; money going out is the normal case and isn't marked.</summary>
    public bool IsIncoming => Transaction.Amount > 0;

    /// <summary>What the row shows when it is open (work/045-transaction-details/spec.md). Worked out when asked, so
    /// two rows of the same transaction stay equal.</summary>
    public IReadOnlyList<TransactionDetail> Details => TransactionDetails.Of(Transaction);
}

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

    /// <summary>Income minus spending, signed ("+555,00 €"); categories excluded from spending don't count.</summary>
    [ObservableProperty]
    public partial string Net { get; private set; } = "";

    /// <summary>Whether income minus spending is below zero; zero is not behind.</summary>
    [ObservableProperty]
    public partial bool IsBehind { get; private set; }

    /// <summary>The note on transactions of types DocuSpend doesn't know, or null when there are none.</summary>
    [ObservableProperty]
    public partial string? UnknownTypes { get; private set; }

    /// <summary>Largest first.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<PieSlice> Slices { get; private set; } = [];

    [ObservableProperty]
    public partial bool HasSpending { get; private set; }

    /// <summary>One point for every day of the period, each the balance at the end of that day.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<BalancePoint> BalancePoints { get; private set; } = [];

    /// <summary>Largest total first, whether money went out or came in; equal totals in the order the other screens use.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<CategoryRow> Categories { get; private set; } = [];

    /// <summary>The selected category's transactions, largest amount first, then by booking date, then as the statement lists them.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<TransactionRow> Transactions { get; private set; } = [];

    /// <summary>The transaction selected in the list, which shows "Zaradiť"; cleared whenever the list changes.</summary>
    [ObservableProperty]
    public partial TransactionRow? SelectedTransaction { get; set; }

    /// <summary>What the last change made from the list did, under the card's heading, until the next change; or null.</summary>
    [ObservableProperty]
    public partial string? TransactionsMessage { get; set; }

    /// <summary>That the last change made from the list won't be kept, until the next change; or null.</summary>
    [ObservableProperty]
    public partial string? TransactionsError { get; set; }

    /// <summary>Clears the card's message; choosing another category or opening a statement does too.</summary>
    public void ClearTransactionsMessage() => TransactionsMessage = TransactionsError = null;

    /// <summary>The categorized statement shown, or null before one is shown.</summary>
    public CategorizedStatement? Categorized => _categorized;

    public void Show(Statement statement)
    {
        _statement = statement;
        SelectedCategory = null;
        Refresh();
    }

    /// <summary>Selects a category from the chart's legend, as clicking its slice does.</summary>
    [RelayCommand]
    private void SelectCategory(CategoryId category) => SelectedCategory = category;

    partial void OnConfigChanged(CategorizationConfig value) => Refresh();

    partial void OnSelectedCategoryChanged(CategoryId? value)
    {
        ClearTransactionsMessage();
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
        var net = categorized.IncomeTotal - categorized.SpendingTotal;
        Net = SlovakFormat.SignedAmount(net);
        IsBehind = net < 0;
        var unknown = statement.Transactions.Count(t => t.Type == TransactionType.Unknown);
        UnknownTypes = unknown == 0 ? null : Texts.UnknownTypes(unknown);

        var spending = categorized.Categories.Where(c => c.Spending > 0).ToList();
        var total = spending.Sum(c => c.Spending!.Value);
        Slices = spending
            .OrderByDescending(c => c.Spending)
            .Select(c => new PieSlice(c.Category.Id, c.Category.Name, c.Spending!.Value, c.Spending!.Value / total,
                Look.SliceColors.For(Config.Catalog, c.Category.Id)))
            .ToList();
        HasSpending = Slices.Count > 0;

        BalancePoints = Balances(statement);
        // Largest first, money out and in alike, equal sizes in the order the other screens use
        // (work/047-largest-first/spec.md). The categorization's own list keeps the catalog's order.
        var screens = CatalogOrder.Categories(Config.Catalog).Select((c, i) => (c.Id, i)).ToDictionary(x => x.Id, x => x.i);
        Categories = categorized.Categories
            .Where(c => c.Total.Count > 0)
            .OrderByDescending(c => Math.Abs(c.Total.Amount))
            .ThenBy(c => screens[c.Category.Id])
            .Select(c => new CategoryRow(c.Category.Id, c.Category.Name, c.Total.Count, c.Total.Amount, SlovakFormat.Amount(c.Total.Amount)))
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
        SelectedTransaction = null;
        if (_categorized is not { } categorized || SelectedCategory is not { } selected)
        {
            Transactions = [];
            return;
        }
        var catalog = Config.Catalog;
        Transactions = categorized.Transactions
            .Select((t, index) => (t, index))
            .Where(x => x.t.Placement.Category == selected)
            .OrderByDescending(x => Math.Abs(x.t.Transaction.Amount))
            .ThenBy(x => x.t.Transaction.BookingDate)
            .ThenBy(x => x.index)
            .Select(x =>
            {
                var (t, index) = x;
                var (category, subcategory) = catalog.Describe(t.Placement);
                var transaction = t.Transaction;
                return new TransactionRow(
                    transaction,
                    SlovakFormat.Date(transaction.BookingDate),
                    Describe(transaction),
                    subcategory?.Name ?? category.Name,
                    SlovakFormat.Amount(transaction.Amount),
                    index,
                    t.Placement.Subcategory,
                    t.ByHand,
                    t.Fingerprint);
            })
            .ToList();
    }

    /// <summary>
    /// A transaction as the list shows it. An unknown type and a cash withdrawal put their type first: the description
    /// alone gives no hint why an unknown type is there, and names only the cash machine of a withdrawal.
    /// </summary>
    private static string Describe(Transaction transaction)
    {
        var text = transaction.Description ?? transaction.CounterpartyName;
        var type = transaction.Type switch
        {
            TransactionType.Unknown => transaction.PrintedType,
            TransactionType.CashWithdrawal => Texts.TypeName(TransactionType.CashWithdrawal),
            _ => null,
        };
        return (type, text) switch
        {
            (null, null) => Texts.NoDescription,
            (null, _) => text,
            (_, null) => type,
            _ => type + Texts.TypeSeparator + text,
        };
    }

    private static List<BalancePoint> Balances(Statement statement)
    {
        var byDay = statement.Transactions.GroupBy(t => t.BookingDate).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        var points = new List<BalancePoint>();
        var running = statement.OpeningBalance;
        for (var day = statement.PeriodStart; day <= statement.PeriodEnd; day = day.AddDays(1))
        {
            running += byDay.GetValueOrDefault(day);
            points.Add(new BalancePoint(day, running));
        }
        return points;
    }
}
