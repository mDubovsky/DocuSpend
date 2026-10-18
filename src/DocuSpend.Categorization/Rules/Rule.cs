using DocuSpend.Categorization.Model;

namespace DocuSpend.Categorization.Rules;

public enum RuleSource
{
    /// <summary>Built into DocuSpend.</summary>
    Default,

    /// <summary>Created by the user.</summary>
    User,
}

/// <summary>
/// One condition and the sub-category that matching transactions go to. Rules are tried by <see cref="Priority"/>
/// (1 is the highest) and, for equal numbers, from the newest to the oldest by <see cref="Order"/>.
/// </summary>
/// <param name="Id">Stable; see decision 0004.</param>
/// <param name="Priority">A whole number from 1. User rules may share one; default rules never do.</param>
/// <param name="Order">The order of creation: increasing, unique among user rules. Changing a rule's priority never changes it.</param>
public sealed record Rule(
    RuleId Id,
    RuleSource Source,
    int Priority,
    long Order,
    Condition Condition,
    SubcategoryId Target);
