namespace DocuSpend.Categorization.Model;

/// <summary>What a category's transactions mean for the spending totals.</summary>
public enum CategoryRole
{
    /// <summary>What the money was spent on; counts in the spending totals.</summary>
    Spending,

    /// <summary>Incoming payments; never counts as spending.</summary>
    Income,

    /// <summary>Not spending (moving money between the user's own accounts, investments, …); left out of the totals.</summary>
    Excluded,
}

public sealed record Subcategory(SubcategoryId Id, string Name);

/// <summary>
/// A category with its sub-categories. A transaction is always in a sub-category; the only exceptions are the two
/// Uncategorized buckets, which have none.
/// </summary>
public sealed record Category(
    CategoryId Id,
    string Name,
    CategoryRole Role,
    IReadOnlyList<Subcategory> Subcategories,
    bool IsBuiltIn,
    bool IsUncategorized = false);

/// <summary>The result of a change to the configuration: the changed value, or the reason it was refused.</summary>
public sealed class Outcome<T>
{
    private Outcome(T? value, Refusal? refusal)
    {
        Value = value;
        Refusal = refusal;
    }

    public T? Value { get; }
    public Refusal? Refusal { get; }
    public bool IsSuccess => Refusal is null;

    public static Outcome<T> Ok(T value) => new(value, null);

    public static Outcome<T> Refused(RefusalKind kind, string message) => new(default, new Refusal(kind, message));

    public static Outcome<T> Refused(Refusal refusal) => new(default, refusal);
}
