namespace DocuSpend.Categorization.Model;

public enum RefusalKind
{
    /// <summary>A transaction-type condition without any type.</summary>
    NoTypeChosen,

    /// <summary>A text condition (description, purpose or counterparty name) without text.</summary>
    EmptyText,

    /// <summary>An account condition without an account number.</summary>
    EmptyAccount,

    /// <summary>A variable-symbol condition without a symbol.</summary>
    EmptySymbol,

    /// <summary>An amount condition with no bound, a negative bound, or a lower bound above the upper.</summary>
    InvalidAmountRange,

    /// <summary>A category or sub-category without a name.</summary>
    EmptyName,

    /// <summary>A name that an existing sibling already has, ignoring case and diacritics.</summary>
    DuplicateName,

    /// <summary>A change that refers to a category that does not exist.</summary>
    UnknownCategory,

    /// <summary>A sub-category added to one of the Uncategorized buckets.</summary>
    NoSubcategoriesAllowed,

    /// <summary>A priority number below 1.</summary>
    InvalidPriority,

    /// <summary>A rule whose target is not an existing sub-category.</summary>
    UnknownTarget,

    /// <summary>A change that refers to a user rule that does not exist.</summary>
    UnknownRule,
}

/// <summary>
/// Why a change to the configuration was refused. The kind says what was wrong; the message is for developers and
/// never contains user content. The interface composes the Slovak text from the kind (decision 0003).
/// </summary>
public sealed record Refusal(RefusalKind Kind, string Message);
