using CommunityToolkit.Mvvm.ComponentModel;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation;

/// <summary>The kinds of condition the spike's form offers: free text, a typed number and a fixed choice.</summary>
public enum ConditionKind { Text, Amount, Type }

public sealed record KindOption(ConditionKind Kind, string Label);

public sealed record TextModeOption(TextMode Mode, string Label);

public sealed record TypeOption(TransactionType Type, string Label);

/// <summary>A rule the user asked for: one condition and the sub-category it puts transactions in.</summary>
public sealed record RuleDraft(Condition Condition, SubcategoryId Target);

/// <summary>
/// The form for adding one user rule (spec: adding a rule). It builds a condition and checks it with the
/// categorizer's own validation, so the form and the categorizer can't disagree; the messages are Slovak.
/// </summary>
public sealed partial class RuleFormViewModel : ObservableObject
{
    public RuleFormViewModel(CategoryCatalog catalog)
    {
        // A rule's target is always a sub-category; the two Uncategorized buckets have none.
        Categories = catalog.Categories.Where(c => !c.IsUncategorized).ToList();
        SelectedKind = ConditionKind.Text;
        SelectedTextMode = TextModes[0];
    }

    public IReadOnlyList<KindOption> Kinds { get; } =
    [
        new(ConditionKind.Text, Texts.KindText),
        new(ConditionKind.Amount, Texts.KindAmount),
        new(ConditionKind.Type, Texts.KindType),
    ];

    public IReadOnlyList<TextModeOption> TextModes { get; } =
    [
        new(TextMode.Contains, Texts.ModeContains),
        new(TextMode.EqualTo, Texts.ModeEqualTo),
    ];

    public IReadOnlyList<TypeOption> Types { get; } =
        Enum.GetValues<TransactionType>().Select(t => new TypeOption(t, Texts.TypeName(t))).ToList();

    public IReadOnlyList<Category> Categories { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsText), nameof(IsAmount), nameof(IsType), nameof(SelectedKindOption))]
    public partial ConditionKind SelectedKind { get; set; }

    /// <summary>The selected kind as a list item, for a choice box.</summary>
    public KindOption SelectedKindOption
    {
        get => Kinds.Single(k => k.Kind == SelectedKind);
        set => SelectedKind = value?.Kind ?? ConditionKind.Text;
    }

    public bool IsText => SelectedKind == ConditionKind.Text;
    public bool IsAmount => SelectedKind == ConditionKind.Amount;
    public bool IsType => SelectedKind == ConditionKind.Type;

    [ObservableProperty]
    public partial string Text { get; set; } = "";

    [ObservableProperty]
    public partial TextModeOption SelectedTextMode { get; set; }

    [ObservableProperty]
    public partial string AmountFrom { get; set; } = "";

    [ObservableProperty]
    public partial string AmountTo { get; set; } = "";

    [ObservableProperty]
    public partial TypeOption? SelectedType { get; set; }

    [ObservableProperty]
    public partial Category? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Subcategory> Subcategories { get; private set; } = [];

    [ObservableProperty]
    public partial Subcategory? SelectedSubcategory { get; set; }

    /// <summary>What is wrong with the input, in Slovak, or null.</summary>
    [ObservableProperty]
    public partial string? Error { get; set; }

    /// <summary>What the last added rule did, in Slovak, or null.</summary>
    [ObservableProperty]
    public partial string? Message { get; set; }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        Subcategories = value?.Subcategories ?? [];
        SelectedSubcategory = null;
    }

    /// <summary>The rule the form describes, or null with <see cref="Error"/> saying why not.</summary>
    public RuleDraft? TryBuild()
    {
        Message = null;
        var condition = BuildCondition();
        if (condition is null)
            return null;
        if (condition.Validate() is { } refusal)
            return Fail(Texts.Refusal(refusal.Kind));
        if (SelectedSubcategory is null)
            return Fail(Texts.Refusal(RefusalKind.UnknownTarget));
        Error = null;
        return new RuleDraft(condition, SelectedSubcategory.Id);
    }

    /// <summary>Empties the values after a rule was added; the kind and the target stay, for the next rule.</summary>
    public void Clear()
    {
        Text = AmountFrom = AmountTo = "";
        SelectedType = null;
        Error = null;
    }

    private Condition? BuildCondition()
    {
        switch (SelectedKind)
        {
            case ConditionKind.Text:
                return new TextCondition(Text.Trim(), SelectedTextMode.Mode);
            case ConditionKind.Amount:
                if (!TryAmount(AmountFrom, out var min) || !TryAmount(AmountTo, out var max))
                    return Fail<Condition>(Texts.AmountNotANumber);
                return new AmountCondition(min, max);
            case ConditionKind.Type:
                return new TypeCondition(SelectedType is null ? [] : [SelectedType.Type]);
            default:
                throw new InvalidOperationException($"Unknown kind {SelectedKind}.");
        }
    }

    /// <summary>An empty field is no bound; anything else must be a Slovak amount.</summary>
    private static bool TryAmount(string text, out decimal? amount)
    {
        amount = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;
        if (!SlovakFormat.TryParseAmount(text, out var value))
            return false;
        amount = value;
        return true;
    }

    private RuleDraft? Fail(string message) => Fail<RuleDraft>(message);

    private T? Fail<T>(string message) where T : class
    {
        Error = message;
        return null;
    }
}
