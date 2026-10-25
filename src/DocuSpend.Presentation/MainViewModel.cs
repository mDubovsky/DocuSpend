using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuSpend.Categorization;
using DocuSpend.Categorization.Rules;
using DocuSpend.Presentation.Look;
using DocuSpend.Statements.Reading;

namespace DocuSpend.Presentation;

/// <summary>A user rule in the list: what it matches and where it puts it.</summary>
public sealed record RuleRow(string Condition, string Target);

/// <summary>
/// The window: the start screen or the statement the user opened, the message about a file that wasn't opened, the
/// rule form and the saved rules.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IStatementReader _reader = new VubStatementReader();
    private readonly SpikeRuleStore _store;
    private readonly SettingsStore? _settings;
    private readonly string? _example;
    private readonly List<RuleDraft> _drafts = [];

    /// <param name="settings">Where the choice of look is remembered; without it, the choice lasts only this session.</param>
    /// <param name="example">The example statement's path; without it, or when the file is missing, none is offered.</param>
    public MainViewModel(SpikeRuleStore store, SettingsStore? settings = null, string? example = null)
    {
        _store = store;
        _settings = settings;
        _example = example is not null && File.Exists(example) ? example : null;
        Statement = new StatementViewModel(LoadRules());
        RuleForm = new RuleFormViewModel(Statement.Config.Catalog);
        RulesError = store.IsAvailable ? null : Texts.RulesFileUnreadable;
        Rules = _drafts.Select(Row).ToList();
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

    /// <summary>The saved user rules, oldest first.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<RuleRow> Rules { get; private set; }

    /// <summary>Why the saved rules aren't used, or null.</summary>
    [ObservableProperty]
    public partial string? RulesError { get; private set; }

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
