using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation;

/// <summary>The six kinds of condition a user rule can have (item 003), in the order the form offers them.</summary>
public enum ConditionKind { Text, CounterpartyName, CounterpartyAccount, Amount, Type, VariableSymbol }

public sealed record KindOption(ConditionKind Kind, string Label);

public sealed record TextModeOption(TextMode Mode, string Label);

/// <summary>One transaction type with its tick box; a type condition takes every ticked type.</summary>
public sealed partial class TypeChoice(TransactionType type, string label) : ObservableObject
{
    public TransactionType Type { get; } = type;

    public string Label { get; } = label;

    [ObservableProperty]
    public partial bool IsChecked { get; set; }
}

/// <summary>A rule the user asked for: one condition and the sub-category it puts transactions in.</summary>
public sealed record RuleDraft(Condition Condition, SubcategoryId Target);

/// <summary>
/// The form for one user rule (work/024-edit-rules/spec.md: the form offers every kind of condition). It builds a
/// condition and checks it with the categorizer's own validation, so the form and the categorizer can't disagree;
/// the messages are Slovak.
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
        new(ConditionKind.CounterpartyName, Texts.KindCounterpartyName),
        new(ConditionKind.CounterpartyAccount, Texts.KindCounterpartyAccount),
        new(ConditionKind.Amount, Texts.KindAmount),
        new(ConditionKind.Type, Texts.KindType),
        new(ConditionKind.VariableSymbol, Texts.KindVariableSymbol),
    ];

    public IReadOnlyList<TextModeOption> TextModes { get; } =
    [
        new(TextMode.Contains, Texts.ModeContains),
        new(TextMode.EqualTo, Texts.ModeEqualTo),
    ];

    public IReadOnlyList<TypeChoice> Types { get; } =
        Enum.GetValues<TransactionType>().Select(t => new TypeChoice(t, Texts.TypeName(t))).ToList();

    public IReadOnlyList<Category> Categories { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsText), nameof(IsAccount), nameof(IsAmount), nameof(IsType), nameof(IsSymbol), nameof(SelectedKindOption))]
    public partial ConditionKind SelectedKind { get; set; }

    /// <summary>The selected kind as a list item, for a choice box.</summary>
    public KindOption SelectedKindOption
    {
        get => Kinds.Single(k => k.Kind == SelectedKind);
        set => SelectedKind = value?.Kind ?? ConditionKind.Text;
    }

    /// <summary>The mode and the text show: for text in the description and for the counterparty's name.</summary>
    public bool IsText => SelectedKind is ConditionKind.Text or ConditionKind.CounterpartyName;
    public bool IsAccount => SelectedKind == ConditionKind.CounterpartyAccount;
    public bool IsAmount => SelectedKind == ConditionKind.Amount;
    public bool IsType => SelectedKind == ConditionKind.Type;
    public bool IsSymbol => SelectedKind == ConditionKind.VariableSymbol;

    [ObservableProperty]
    public partial string Text { get; set; } = "";

    [ObservableProperty]
    public partial TextModeOption SelectedTextMode { get; set; }

    [ObservableProperty]
    public partial string Account { get; set; } = "";

    [ObservableProperty]
    public partial string AmountFrom { get; set; } = "";

    [ObservableProperty]
    public partial string AmountTo { get; set; } = "";

    [ObservableProperty]
    public partial string Symbol { get; set; } = "";

    [ObservableProperty]
    public partial Category? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Subcategory> Subcategories { get; private set; } = [];

    [ObservableProperty]
    public partial Subcategory? SelectedSubcategory { get; set; }

    /// <summary>What is wrong with the input, in Slovak, or null.</summary>
    [ObservableProperty]
    public partial string? Error { get; set; }

    /// <summary>What the last added or changed rule did, in Slovak, or null.</summary>
    [ObservableProperty]
    public partial string? Message { get; set; }

    /// <summary>The rule open in the form, or null while the form adds a new rule (spec: editing a rule).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing), nameof(Heading))]
    public partial RuleId? EditingRule { get; private set; }

    public bool IsEditing => EditingRule is not null;

    public string Heading => IsEditing ? Texts.EditRule : Texts.NewRule;

    /// <summary>The rule's priority number as typed; used only while editing.</summary>
    [ObservableProperty]
    public partial string Priority { get; set; } = "";

    /// <summary>Opens a user rule in the form: its condition, target and number, ready to be changed.</summary>
    public void Edit(Rule rule)
    {
        Reset();
        switch (rule.Condition)
        {
            case TextCondition text:
                SelectedKind = ConditionKind.Text;
                (Text, SelectedTextMode) = (text.Text, TextModes.Single(m => m.Mode == text.Mode));
                break;
            case CounterpartyNameCondition name:
                SelectedKind = ConditionKind.CounterpartyName;
                (Text, SelectedTextMode) = (name.Text, TextModes.Single(m => m.Mode == name.Mode));
                break;
            case CounterpartyAccountCondition account:
                SelectedKind = ConditionKind.CounterpartyAccount;
                Account = account.Account;
                break;
            case AmountCondition amount:
                SelectedKind = ConditionKind.Amount;
                AmountFrom = amount.Min is { } min ? SlovakFormat.AmountInput(min) : "";
                AmountTo = amount.Max is { } max ? SlovakFormat.AmountInput(max) : "";
                break;
            case TypeCondition type:
                SelectedKind = ConditionKind.Type;
                foreach (var choice in Types)
                    choice.IsChecked = type.Types.Contains(choice.Type);
                break;
            case VariableSymbolCondition symbol:
                SelectedKind = ConditionKind.VariableSymbol;
                Symbol = symbol.Symbol;
                break;
            default:
                throw new NotSupportedException($"The form can't show {rule.Condition.GetType().Name}.");
        }
        SelectedCategory = Categories.FirstOrDefault(c => c.Subcategories.Any(s => s.Id == rule.Target));
        SelectedSubcategory = Subcategories.FirstOrDefault(s => s.Id == rule.Target);
        Priority = rule.Priority.ToString(SlovakFormat.Culture);
        EditingRule = rule.Id;
    }

    /// <summary>
    /// The typed priority: a whole number from 1 to <see cref="CategorizationConfig.MaxUserPriority"/>, spaces around
    /// it allowed; or null with <see cref="Error"/> saying why not.
    /// </summary>
    public int? TryPriority()
    {
        if (int.TryParse(Priority.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            && number is >= 1 and <= CategorizationConfig.MaxUserPriority)
            return number;
        Error = Texts.Refusal(RefusalKind.InvalidPriority);
        return null;
    }

    /// <summary>Back to an empty "Nové pravidlo": no rule open, no values, no target. Messages are left to the caller.</summary>
    public void Reset()
    {
        Clear();
        EditingRule = null;
        Priority = "";
        SelectedKind = ConditionKind.Text;
        SelectedTextMode = TextModes[0];
        SelectedCategory = null;
    }

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
        Text = Account = AmountFrom = AmountTo = Symbol = "";
        foreach (var type in Types)
            type.IsChecked = false;
        Error = null;
    }

    private Condition? BuildCondition()
    {
        switch (SelectedKind)
        {
            case ConditionKind.Text:
                return new TextCondition(Text.Trim(), SelectedTextMode.Mode);
            case ConditionKind.CounterpartyName:
                return new CounterpartyNameCondition(Text.Trim(), SelectedTextMode.Mode);
            case ConditionKind.CounterpartyAccount:
                return new CounterpartyAccountCondition(Account.Trim());
            case ConditionKind.Amount:
                if (!TryAmount(AmountFrom, out var min) || !TryAmount(AmountTo, out var max))
                    return Fail<Condition>(Texts.AmountNotANumber);
                return new AmountCondition(min, max);
            case ConditionKind.Type:
                return new TypeCondition(Types.Where(t => t.IsChecked).Select(t => t.Type));
            case ConditionKind.VariableSymbol:
                return new VariableSymbolCondition(Symbol.Trim());
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
