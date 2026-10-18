using DocuSpend.Categorization.Model;
using DocuSpend.Statements.Model;

namespace DocuSpend.Categorization.Rules;

/// <summary>How a text condition compares: the text is contained in the transaction's text, or equals it.</summary>
public enum TextMode
{
    Contains,
    EqualTo,
}

/// <summary>
/// What a rule tests; exactly one per rule. A condition on a field the transaction doesn't have never matches it.
/// Conditions are plain data, so saving and loading them later is straightforward.
/// </summary>
public abstract record Condition
{
    public abstract bool Matches(Transaction transaction);

    /// <summary>Why the condition can't be used as given, or <c>null</c> when it is complete and sensible.</summary>
    public abstract Refusal? Validate();

    protected static Refusal? Needs(bool ok, RefusalKind kind, string message) => ok ? null : new Refusal(kind, message);
}

/// <summary>The transaction's type is any of the chosen ones.</summary>
public sealed record TypeCondition : Condition
{
    public TypeCondition(IEnumerable<TransactionType> types) => Types = types.Distinct().Order().ToArray();

    public IReadOnlyList<TransactionType> Types { get; }

    public override bool Matches(Transaction transaction) => Types.Contains(transaction.Type);

    public override Refusal? Validate() =>
        Needs(Types.Count > 0, RefusalKind.NoTypeChosen, "A transaction-type condition needs at least one type.");

    public bool Equals(TypeCondition? other) => other is not null && Types.SequenceEqual(other.Types);

    public override int GetHashCode() => Types.Aggregate(0, (hash, type) => HashCode.Combine(hash, type));
}

/// <summary>The transaction's description or its payment purpose contains the text, or equals it.</summary>
public sealed record TextCondition(string Text, TextMode Mode) : Condition
{
    public override bool Matches(Transaction transaction) =>
        Test(Mode, transaction.Description, Text) || Test(Mode, transaction.PaymentPurpose, Text);

    public override Refusal? Validate() =>
        Needs(!TextMatching.HasNoWords(Text), RefusalKind.EmptyText, "A text condition needs text with letters or digits.");

    internal static bool Test(TextMode mode, string? value, string text) => mode switch
    {
        TextMode.Contains => TextMatching.Contains(value, text),
        TextMode.EqualTo => TextMatching.Equal(value, text),
        _ => false,
    };
}

/// <summary>The counterparty's name contains the text, or equals it.</summary>
public sealed record CounterpartyNameCondition(string Text, TextMode Mode) : Condition
{
    public override bool Matches(Transaction transaction) => TextCondition.Test(Mode, transaction.CounterpartyName, Text);

    public override Refusal? Validate() =>
        Needs(!TextMatching.HasNoWords(Text), RefusalKind.EmptyText, "A counterparty-name condition needs text with letters or digits.");
}

/// <summary>The counterparty's account number equals the given one, ignoring spaces and case.</summary>
public sealed record CounterpartyAccountCondition(string Account) : Condition
{
    public override bool Matches(Transaction transaction) => TextMatching.SameAccount(transaction.CounterpartyAccount, Account);

    public override Refusal? Validate() =>
        Needs(!TextMatching.IsBlank(Account), RefusalKind.EmptyAccount, "An account condition needs an account number.");
}

/// <summary>
/// The size of the amount, whether money comes in or goes out, is at least <see cref="Min"/> and at most
/// <see cref="Max"/>, both inclusive. Either bound may be left out, but not both.
/// </summary>
public sealed record AmountCondition(decimal? Min, decimal? Max) : Condition
{
    public override bool Matches(Transaction transaction)
    {
        var size = Math.Abs(transaction.Amount);
        return (Min is null || size >= Min) && (Max is null || size <= Max);
    }

    public override Refusal? Validate() =>
        Needs(
            (Min is not null || Max is not null) && Min is null or >= 0 && Max is null or >= 0 && !(Min > Max),
            RefusalKind.InvalidAmountRange,
            "An amount condition needs a lower bound, an upper bound or both, none negative, and the lower not above the upper.");
}

/// <summary>The transaction's variable symbol equals the given one exactly.</summary>
public sealed record VariableSymbolCondition(string Symbol) : Condition
{
    public override bool Matches(Transaction transaction) =>
        transaction.VariableSymbol is { } symbol && string.Equals(symbol, Symbol, StringComparison.Ordinal);

    public override Refusal? Validate() =>
        Needs(!TextMatching.IsBlank(Symbol), RefusalKind.EmptySymbol, "A variable-symbol condition needs a symbol.");
}
