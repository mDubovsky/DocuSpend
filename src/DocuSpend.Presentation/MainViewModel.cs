using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Presentation.Look;
using DocuSpend.Statements.Reading;

namespace DocuSpend.Presentation;

/// <summary>A rule in a list: its number, what it matches and where it puts it.</summary>
public sealed record RuleRow(RuleId Id, string Priority, string Condition, string Target, bool IsEditing = false);

/// <summary>
/// The window: the start screen or the statement the user opened, the message about a file that wasn't opened, the
/// rule form and the saved rules.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IStatementReader _reader = new VubStatementReader();
    private readonly RuleStore _store;
    private readonly SettingsStore? _settings;
    private readonly string? _example;

    /// <param name="settings">Where the choice of look is remembered; without it, the choice lasts only this session.</param>
    /// <param name="example">The example statement's path; without it, or when the file is missing, none is offered.</param>
    public MainViewModel(RuleStore store, SettingsStore? settings = null, string? example = null)
    {
        _store = store;
        _settings = settings;
        _example = example is not null && File.Exists(example) ? example : null;
        var load = store.Load(CategorizationConfig.Default);
        Statement = new StatementViewModel(load.Config);
        RuleForm = new RuleFormViewModel(Statement.Config.Catalog);
        RulesMessage = Texts.RulesProblemMessage(load.Problem, load.MovedTo);
        RulesNotSaving = !store.IsSaving;
        Rules = UserRows();
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

    public StatementViewModel Statement { get; }

    public RuleFormViewModel RuleForm { get; }

    /// <summary>Where "Otvoriť výpis…" asks for a file; the window sets it.</summary>
    public IStatementPicker? Picker { get; set; }

    /// <summary>False on the start screen; true once a statement has been opened.</summary>
    [ObservableProperty]
    public partial bool HasStatement { get; private set; }

    /// <summary>The statement shown is the example, with invented data, and is labelled so.</summary>
    [ObservableProperty]
    public partial bool IsExample { get; private set; }

    /// <summary>A file is being read; opening another waits until it is done.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand), nameof(OpenExampleCommand))]
    public partial bool IsReading { get; private set; }

    /// <summary>A file is being dragged over the window; the window sets it.</summary>
    [ObservableProperty]
    public partial bool IsDraggingOver { get; set; }

    /// <summary>Why the last file wasn't opened, or why a drop wasn't, until closed or replaced; or null.</summary>
    [ObservableProperty]
    public partial string? OpenMessage { get; private set; }

    public bool HasExample => _example is not null;

    private bool CanOpen() => !IsReading;

    private bool CanOpenExample() => !IsReading && _example is not null;

    /// <summary>Asks the picker for a file and opens it; cancelling changes nothing.</summary>
    [RelayCommand(CanExecute = nameof(CanOpen))]
    private async Task OpenAsync()
    {
        if (Picker is null || await Picker.PickAsync() is not { } path)
            return;
        await OpenFileAsync(path);
    }

    [RelayCommand(CanExecute = nameof(CanOpenExample))]
    private Task OpenExampleAsync() => _example is null ? Task.CompletedTask : OpenFileAsync(_example, isExample: true);

    [RelayCommand]
    private void CloseMessage() => OpenMessage = null;

    /// <summary>
    /// Reads the file and shows its statement, replacing what was shown. A file that can't be read changes nothing
    /// that is shown and sets the message instead. Ignored while another file is being read.
    /// </summary>
    public async Task OpenFileAsync(string path, bool isExample = false)
    {
        if (IsReading)
            return;
        IsReading = true;
        try
        {
            var content = await StatementFile.ReadAsync(path);
            var problem = content.Problem;
            Statements.Model.Statement? statement = null;
            if (problem is null)
            {
                var result = await Task.Run(() => _reader.Read(content.Bytes!));
                statement = result.Statement;
                if (result.Rejection is { } rejection)
                    problem = OpenProblems.From(rejection.Kind);
            }
            if (statement is null)
            {
                OpenMessage = Texts.NotOpenedMessage(problem!.Value, previousStatementKept: HasStatement);
                return;
            }
            Statement.Show(statement);
            HasStatement = true;
            IsExample = isExample;
            OpenMessage = null;
        }
        finally
        {
            IsReading = false;
        }
    }

    /// <summary>Opens a dropped file; several files, a folder or anything else give their message and change nothing.</summary>
    public Task DropAsync(DroppedContent dropped)
    {
        IsDraggingOver = false;
        if (IsReading)
            return Task.CompletedTask;
        if (dropped.Files.Count == 1 && dropped.Others == 0)
            return OpenFileAsync(dropped.Files[0]);
        OpenMessage = dropped.Files.Count > 1 ? Texts.DropSeveralFiles : Texts.DropNotAFile;
        return Task.CompletedTask;
    }

    /// <summary>The user rules in the order they are tried: the one higher up wins (spec: "Moje pravidlá").</summary>
    [ObservableProperty]
    public partial IReadOnlyList<RuleRow> Rules { get; private set; }

    /// <summary>Why the saved rules weren't used at start, until closed; or null (spec: when the saved rules can't be used).</summary>
    [ObservableProperty]
    public partial string? RulesMessage { get; private set; }

    /// <summary>The saved rules couldn't be opened or are from a newer DocuSpend, so changes aren't saved this session.</summary>
    [ObservableProperty]
    public partial bool RulesNotSaving { get; private set; }

    [RelayCommand]
    private void CloseRulesMessage() => RulesMessage = null;

    /// <summary>
    /// Adds the form's rule on top of every earlier one, saves it, and categorizes the statement again. A rule that
    /// can't be saved still applies until DocuSpend closes, and the form says so. Invalid input changes nothing.
    /// </summary>
    [RelayCommand]
    private void AddRule()
    {
        // Enter in the form presses its default button; while a rule is open, that means saving it.
        if (RuleForm.IsEditing)
        {
            SaveRule();
            return;
        }
        if (RuleForm.TryBuild() is not { } draft)
            return;
        var outcome = Statement.Config.AddUserRuleOnTop(draft.Condition, draft.Target);
        if (!outcome.IsSuccess)
        {
            RuleForm.Error = Texts.Refusal(outcome.Refusal!.Kind);
            return;
        }
        var saved = ApplyUserChange(outcome.Value!);

        RuleForm.Clear();
        Confirm(Statement.Config.UserRules[^1].Id, saved, Texts.RuleOnlyThisSession, Texts.RuleSaveFailed);
    }

    /// <summary>Opens a user rule in the form (spec: editing a rule). Whatever was typed in the form is replaced.</summary>
    [RelayCommand]
    private void EditRule(RuleRow row)
    {
        if (Statement.Config.UserRules.FirstOrDefault(r => r.Id == row.Id) is not { } rule)
            return;
        RuleForm.Edit(rule);
        Rules = UserRows();
    }

    /// <summary>
    /// Changes the open rule as the form describes it, saves it, and categorizes the statement again. It keeps its
    /// identifier and creation order. Invalid input changes nothing and leaves the form open.
    /// </summary>
    [RelayCommand]
    private void SaveRule()
    {
        if (RuleForm.EditingRule is not { } id || RuleForm.TryBuild() is not { } draft || RuleForm.TryPriority() is not { } priority)
            return;
        var outcome = Statement.Config.ChangeUserRule(id, draft.Condition, draft.Target, priority);
        if (!outcome.IsSuccess)
        {
            RuleForm.Error = Texts.Refusal(outcome.Refusal!.Kind);
            return;
        }
        RuleForm.Reset();
        var saved = ApplyUserChange(outcome.Value!);
        Confirm(id, saved, Texts.ChangeOnlyThisSession, Texts.ChangeSaveFailed);
    }

    /// <summary>Closes the open rule without changing it; the form is an empty "Nové pravidlo" again.</summary>
    [RelayCommand]
    private void CancelEdit()
    {
        RuleForm.Reset();
        RuleForm.Message = null;
        Rules = UserRows();
    }

    /// <summary>
    /// The form's confirmation after a rule was added or changed: how many transactions it now catches, and whether
    /// it will be kept.
    /// </summary>
    private void Confirm(RuleId rule, bool saved, string onlyThisSession, string saveFailed)
    {
        var matched = Statement.Categorized?.Transactions.Count(t => t.Rule?.Id == rule) ?? 0;
        RuleForm.Message = matched > 0 ? Texts.RuleMatched(matched) : saved ? Texts.RuleMatchedNothing : Texts.RuleMatchedNothingUnsaved;
        RuleForm.Error = saved ? null : RulesNotSaving ? onlyThisSession : saveFailed;
    }

    /// <summary>
    /// Saves a change to the user's rules or categories, then shows it. Every such change goes through here, so each
    /// is saved the same way (spec: saving). False when it wasn't saved; it applies until DocuSpend closes anyway.
    /// </summary>
    private bool ApplyUserChange(CategorizationConfig config)
    {
        var saved = _store.Save(config);
        Statement.Config = config;
        Rules = UserRows();
        return saved;
    }

    private List<RuleRow> UserRows() =>
        Statement.Config.RulesInOrder().Where(r => r.Source == RuleSource.User).Select(r => Row(r, r.Priority.ToString(SlovakFormat.Culture))).ToList();

    private RuleRow Row(Rule rule, string priority)
    {
        var (category, subcategory) = Statement.Config.Catalog.FindSubcategory(rule.Target)!.Value;
        return new RuleRow(rule.Id, priority, Describe(rule.Condition), $"{category.Name} › {subcategory.Name}",
            IsEditing: rule.Id == RuleForm.EditingRule);
    }

    /// <summary>A condition in Slovak, as the rules list shows it.</summary>
    public static string Describe(Condition condition) => condition switch
    {
        TextCondition { Mode: TextMode.Contains } text => $"Text obsahuje „{text.Text}“",
        TextCondition text => $"Text sa rovná „{text.Text}“",
        CounterpartyNameCondition { Mode: TextMode.Contains } name => $"Protistrana obsahuje „{name.Text}“",
        CounterpartyNameCondition name => $"Protistrana sa rovná „{name.Text}“",
        CounterpartyAccountCondition account => $"Účet protistrany: {account.Account}",
        AmountCondition { Min: { } min, Max: { } max } => $"Suma od {SlovakFormat.Amount(min)} do {SlovakFormat.Amount(max)}",
        AmountCondition { Min: { } min } => $"Suma od {SlovakFormat.Amount(min)}",
        AmountCondition { Max: { } max } => $"Suma do {SlovakFormat.Amount(max)}",
        TypeCondition type => "Typ: " + string.Join(", ", type.Types.Select(Texts.TypeName)),
        VariableSymbolCondition symbol => $"Variabilný symbol: {symbol.Symbol}",
        _ => throw new NotSupportedException($"No description for {condition.GetType().Name}."),
    };
}
