namespace DocuSpend.Categorization.Model;

// Rules and results refer to categories by identifier, never by name, so a built-in name can be corrected later
// without breaking anyone's saved rules (docs/decisions/0004-stable-category-and-rule-identifiers.md).
// Built-in identifiers are fixed English keys such as "housing.rent"; user-created ones are generated.

public readonly record struct CategoryId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct SubcategoryId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct RuleId(string Value)
{
    public override string ToString() => Value;
}

/// <summary>Where a transaction is: a sub-category, or, for the two Uncategorized buckets, just the category.</summary>
public readonly record struct Placement(CategoryId Category, SubcategoryId? Subcategory);
