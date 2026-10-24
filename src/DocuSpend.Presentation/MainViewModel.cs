using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuSpend.Categorization;
using DocuSpend.Categorization.Rules;
using DocuSpend.Presentation.Look;
using DocuSpend.Statements.Reading;

namespace DocuSpend.Presentation;

/// <summary>A user rule in the list: what it matches and where it puts it.</summary>
public sealed record RuleRow(string Condition, string Target);

/// <summary>The window: the samples to choose from, the statement shown, the rule form and the saved rules.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IStatementReader _reader = new VubStatementReader();
    private readonly SpikeRuleStore _store;
    private readonly SettingsStore? _settings;
    private readonly List<RuleDraft> _drafts = [];

    /// <param name="settings">Where the choice of look is remembered; without it, the choice lasts only this session.</param>
    public MainViewModel(IReadOnlyList<SampleItem> samples, SpikeRuleStore store, SettingsStore? settings = null)
    {
        _store = store;
        _settings = settings;
        Samples = samples;
        Statement = new StatementViewModel(LoadRules());
        RuleForm = new RuleFormViewModel(Statement.Config.Catalog);
        RulesError = store.IsAvailable ? null : Texts.RulesFileUnreadable;
        Rules = _drafts.Select(Row).ToList();
        SelectedSample = samples.FirstOrDefault();
        var saved = settings?.LoadLook() ?? LookChoice.System;
        _loadingLook = true;
        SelectedLook = Looks.Single(l => l.Choice == saved);
        _loadingLook = false;
    }

    private readonly bool _loadingLook;

    public IReadOnlyList<LookOption> Looks { get; } =
    [
        new(LookChoice.System, Texts.LookSystem),
        new(LookChoice.Light, Texts.LookLight),
        new(LookChoice.Dark, Texts.LookDark),
    ];

    /// <summary>The look chosen in the header; saved as soon as it changes.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Look))]
    public partial LookOption SelectedLook { get; set; }

    public LookChoice Look => SelectedLook.Choice;

    partial void OnSelectedLookChanged(LookOption value)
    {
        if (!_loadingLook)
            _settings?.SaveLook(value.Choice);
    }

    public IReadOnlyList<SampleItem> Samples { get; }

    public StatementViewModel Statement { get; }

    public RuleFormViewModel RuleForm { get; }

    [ObservableProperty]
    public partial SampleItem? SelectedSample { get; set; }

    /// <summary>Why the chosen sample isn't shown, or null.</summary>
    [ObservableProperty]
    public partial string? Error { get; private set; }

    /// <summary>The saved user rules, oldest first.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<RuleRow> Rules { get; private set; }

    /// <summary>Why the saved rules aren't used, or null.</summary>
    [ObservableProperty]
    public partial string? RulesError { get; private set; }

    partial void OnSelectedSampleChanged(SampleItem? value)
    {
        if (value is null)
            return;
        var result = _reader.Read(File.ReadAllBytes(value.Path));
        Error = result.IsSuccess ? null : Texts.StatementUnreadable;
        if (result.Statement is { } statement)
            Statement.Show(statement);
    }

    /// <summary>
    /// Adds the form's rule on top of every earlier one, saves it, and categorizes the statement again. Nothing is
    /// saved when the input is invalid or the rules file couldn't be read.
    /// </summary>
    [RelayCommand]
    private void AddRule()
    {
        if (RuleForm.TryBuild() is not { } draft)
            return;
        if (!_store.IsAvailable)
        {
            RuleForm.Error = Texts.RulesCannotBeSaved;
            return;
        }
        var outcome = Statement.Config.AddUserRuleOnTop(draft.Condition, draft.Target);
        if (!outcome.IsSuccess)
        {
            RuleForm.Error = Texts.Refusal(outcome.Refusal!.Kind);
            return;
        }
        if (!_store.Save([.. _drafts, draft]))
        {
            RuleForm.Error = Texts.RuleNotSaved;
            return;
        }
        _drafts.Add(draft);
        Rules = _drafts.Select(Row).ToList();
        var config = outcome.Value!;
        Statement.Config = config;

        var added = config.UserRules[^1].Id;
        var matched = Statement.Categorized?.Transactions.Count(t => t.Rule?.Id == added) ?? 0;
        RuleForm.Clear();
        RuleForm.Message = matched == 0 ? Texts.RuleMatchedNothing : Texts.RuleMatched(matched);
    }

    /// <summary>The saved rules, each added on top of the ones before, which reproduces the order they were added in.</summary>
    private CategorizationConfig LoadRules()
    {
        var config = CategorizationConfig.Default;
        foreach (var draft in _store.Load())
        {
            var outcome = config.AddUserRuleOnTop(draft.Condition, draft.Target);
            if (!outcome.IsSuccess)
            {
                // A rule the categorizer refuses (a target that doesn't exist) makes the whole file unreadable.
                _store.MarkUnavailable();
                _drafts.Clear();
                return CategorizationConfig.Default;
            }
            config = outcome.Value!;
            _drafts.Add(draft);
        }
        return config;
    }

    private RuleRow Row(RuleDraft draft)
    {
        var (category, subcategory) = Statement.Config.Catalog.FindSubcategory(draft.Target)!.Value;
        return new RuleRow(Describe(draft.Condition), $"{category.Name} › {subcategory.Name}");
    }

    /// <summary>A condition in Slovak, as the rules list shows it.</summary>
    public static string Describe(Condition condition) => condition switch
    {
        TextCondition { Mode: TextMode.Contains } text => $"Text obsahuje „{text.Text}“",
        TextCondition text => $"Text sa rovná „{text.Text}“",
        AmountCondition { Min: { } min, Max: { } max } => $"Suma od {SlovakFormat.Amount(min)} do {SlovakFormat.Amount(max)}",
        AmountCondition { Min: { } min } => $"Suma od {SlovakFormat.Amount(min)}",
        AmountCondition { Max: { } max } => $"Suma do {SlovakFormat.Amount(max)}",
        TypeCondition type => "Typ: " + string.Join(", ", type.Types.Select(Texts.TypeName)),
        _ => condition.GetType().Name,
    };
}
