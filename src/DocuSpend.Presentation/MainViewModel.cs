using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Presentation.Look;
using DocuSpend.Statements.Reading;

namespace DocuSpend.Presentation;

/// <summary>The two lists the rules card switches between (spec: the rules card).</summary>
public enum RuleView { Mine, BuiltIn }

public sealed record RuleViewOption(RuleView View, string Label);

/// <summary>The two screens a statement is shown on: the statement itself and its rules (spec of #21).</summary>
public enum AppScreen { Statement, Rules, Categories }

public sealed record AppScreenOption(AppScreen Screen, string Label);

/// <summary>
/// A rule in a list: what it matches and where it puts it. <paramref name="Priority"/> is the user rule's number,
/// which the list keeps for its order but shows only in the form; empty for a built-in rule.
/// </summary>
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
        Categories = new CategoriesViewModel(Statement.Config.Catalog);
        RulesMessage = Texts.RulesProblemMessage(load.Problem, load.MovedTo);
        RulesNotSaving = !store.IsSaving;
        Rules = UserRows();
        BuiltInRules = Statement.Config.DefaultRules.Select(r => Row(r, "")).ToList();
        SelectedRuleView = RuleViews[0];
        SelectedScreen = Screens[0];
        var saved = settings?.LoadLook() ?? LookChoice.System;
        _loadingLook = true;
        SelectedLook = Looks.Single(l => l.Choice == saved);
        _loadingLook = false;
        _hideFingerprintNote = settings?.LoadHideFingerprintNote() ?? false;
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

    /// <summary>The "Kategórie" screen.</summary>
    public CategoriesViewModel Categories { get; }

    public IReadOnlyList<AppScreenOption> Screens { get; } =
    [
        new(AppScreen.Statement, Texts.StatementScreen),
        new(AppScreen.Rules, Texts.RulesScreen),
        new(AppScreen.Categories, Texts.Categories),
    ];

    /// <summary>Which screen a statement is shown on: "Výpis" at start, and after every statement that is opened.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsStatement), nameof(ShowsRules), nameof(ShowsCategories))]
    public partial AppScreenOption SelectedScreen { get; set; }

    /// <summary>The statement screen shows: a statement is open and "Výpis" is chosen.</summary>
    public bool ShowsStatement => HasStatement && SelectedScreen.Screen == AppScreen.Statement;

    /// <summary>The rules screen shows: a statement is open and "Pravidlá" is chosen.</summary>
    public bool ShowsRules => HasStatement && SelectedScreen.Screen == AppScreen.Rules;

    /// <summary>The categories screen shows: a statement is open and "Kategórie" is chosen.</summary>
    public bool ShowsCategories => HasStatement && SelectedScreen.Screen == AppScreen.Categories;

    /// <summary>Where "Otvoriť výpis…" asks for a file; the window sets it.</summary>
    public IStatementPicker? Picker { get; set; }

    /// <summary>False on the start screen; true once a statement has been opened.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsStatement), nameof(ShowsRules), nameof(ShowsCategories))]
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
            SelectedScreen = Screens[0];
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

    /// <summary>
    /// The default rules in the order they are tried, without numbers: every user rule wins over them (spec: the rules
    /// card). They never change while DocuSpend runs.
    /// </summary>
    public IReadOnlyList<RuleRow> BuiltInRules { get; }

    public IReadOnlyList<RuleViewOption> RuleViews { get; } =
    [
        new(RuleView.Mine, Texts.Rules),
        new(RuleView.BuiltIn, Texts.BuiltInRules),
    ];

    /// <summary>Which list the rules card shows: the user's own at start, or the built-in ones.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBuiltInView))]
    public partial RuleViewOption SelectedRuleView { get; set; }

    public bool IsBuiltInView => SelectedRuleView.View == RuleView.BuiltIn;

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
    /// "Pridať kategóriu": adds the typed category with the chosen role, saves it and shows it. A refused name changes
    /// nothing and stays typed, with the reason under it. A category that can't be saved still works until DocuSpend
    /// closes, and the card says so.
    /// </summary>
    [RelayCommand]
    private void AddCategory()
    {
        var card = Categories;
        card.Message = card.Error = card.NameError = null;
        var outcome = Statement.Config.AddCategory(card.NewName, card.SelectedRole.Role);
        if (!outcome.IsSuccess)
        {
            card.NameError = Texts.Refusal(outcome.Refusal!.Kind);
            return;
        }
        var name = card.NewName.Trim();
        var saved = ApplyUserChange(outcome.Value!);
        ShowCategories();
        card.Added(outcome.Value!.Catalog.Categories[^1].Id);
        Report(saved, Texts.CategoryAdded(name), Texts.CategoryOnlyThisSession, Texts.CategorySaveFailed);
    }

    /// <summary>
    /// "Pridať podkategóriu" in the chosen category's details: adds the typed sub-category to it, saves it and shows it. A refused name
    /// changes nothing and stays typed, with the reason under the field.
    /// </summary>
    [RelayCommand]
    private void AddSubcategory()
    {
        var card = Categories;
        if (card.SelectedRow is not { } row || !row.CanAddSubcategory)
            return;
        card.Message = card.Error = card.SubcategoryError = null;
        var outcome = Statement.Config.AddSubcategory(row.Id, card.SubcategoryName);
        if (!outcome.IsSuccess)
        {
            card.SubcategoryError = Texts.Refusal(outcome.Refusal!.Kind);
            return;
        }
        var name = card.SubcategoryName.Trim();
        var saved = ApplyUserChange(outcome.Value!);
        card.SubcategoryName = "";
        ShowCategories();
        Report(saved, Texts.SubcategoryAdded(name, row.Name), Texts.SubcategoryOnlyThisSession, Texts.SubcategorySaveFailed);
    }

    /// <summary>The card and the rule form show the catalog as it is now.</summary>
    private void ShowCategories()
    {
        Categories.Refresh(Statement.Config.Catalog);
        RuleForm.UseCatalog(Statement.Config.Catalog);
    }

    /// <summary>The card's answer to an addition: the confirmation, or why it won't last.</summary>
    private void Report(bool saved, string confirmation, string onlyThisSession, string saveFailed)
    {
        Categories.Message = saved ? confirmation : null;
        Categories.Error = saved ? null : RulesNotSaving ? onlyThisSession : saveFailed;
    }

    /// <summary>
    /// Adds the form's rule on top of every earlier one, saves it, and categorizes the statement again. A rule that
    /// can't be saved still applies until DocuSpend closes, and the form says so. Invalid input changes nothing.
    /// </summary>
    [RelayCommand]
    private void AddRule()
    {
        TakeBackQuestion();
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
        TakeBackQuestion();
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
        TakeBackQuestion();
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
        TakeBackQuestion();
        RuleForm.Reset();
        RuleForm.Message = null;
        Rules = UserRows();
    }

    /// <summary>
    /// The rule the pop-up asks about, "Odstrániť toto pravidlo?", or null when no question is open (spec: deleting a
    /// rule). While it is open the rest of the window waits.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(KeepRuleCommand))]
    public partial RuleRow? RuleToDelete { get; private set; }

    /// <summary>What the last deletion did, in "Moje pravidlá", until the next change; or null (spec: deleting a rule).</summary>
    [ObservableProperty]
    public partial string? RulesListMessage { get; private set; }

    /// <summary>That the last deletion won't be kept, until the next change; or null.</summary>
    [ObservableProperty]
    public partial string? RulesListError { get; private set; }

    /// <summary>"Odstrániť" in a row: a pop-up asks first, and nothing is deleted yet.</summary>
    [RelayCommand]
    private void AskToDelete(RuleRow row) => RuleToDelete = row;

    /// <summary>"Ponechať", or Escape: the pop-up closes and the rule stays.</summary>
    [RelayCommand(CanExecute = nameof(IsAsking))]
    private void KeepRule() => TakeBackQuestion();

    private bool IsAsking() => RuleToDelete is not null;

    /// <summary>
    /// "Odstrániť" after the question: deletes the rule, saves, and categorizes the statement again. A deletion that
    /// can't be saved still applies until DocuSpend closes, and the list says the rule will come back.
    /// </summary>
    [RelayCommand]
    private void DeleteRule()
    {
        if (RuleToDelete?.Id is not { } id)
            return;
        TakeBackQuestion();
        var outcome = Statement.Config.RemoveUserRule(id);
        if (!outcome.IsSuccess)
        {
            Rules = UserRows();
            return;
        }
        if (RuleForm.EditingRule == id)
        {
            RuleForm.Reset();
            RuleForm.Message = null;
        }
        var saved = ApplyUserChange(outcome.Value!);
        RulesListMessage = Texts.RuleDeleted;
        RulesListError = saved ? null : RulesNotSaving ? Texts.DeleteOnlyThisSession : Texts.DeleteSaveFailed;
    }

    /// <summary>Closes an open deletion question; any other change closes it too.</summary>
    private void TakeBackQuestion() => RuleToDelete = null;

    // Categorizing a transaction from the list (work/022-categorize-from-list/spec.md)

    /// <summary>"Túto správu už nezobrazovať" was ticked: hand choices are made without the box about fingerprints.</summary>
    private bool _hideFingerprintNote;

    /// <summary>The "Zaradiť transakciu" box, while it is open; the rest of the window waits.</summary>
    [ObservableProperty]
    public partial CategorizeViewModel? Categorizing { get; private set; }

    /// <summary>"Zaradiť" on a selected row: opens the box, filled in from the transaction.</summary>
    [RelayCommand]
    private void Categorize(TransactionRow row)
    {
        TakeBackQuestion();
        Categorizing = new CategorizeViewModel(row, Statement.Config.Catalog);
    }

    /// <summary>"Zrušiť", or Escape: the box closes and nothing changes.</summary>
    [RelayCommand]
    private void CancelCategorize() => Categorizing = null;

    /// <summary>
    /// "Zaradiť" in the box. For just this transaction, the box about fingerprints opens first unless it was switched
    /// off; for a rule, the condition must be valid and catch this transaction.
    /// </summary>
    [RelayCommand]
    private void ConfirmCategorize()
    {
        if (Categorizing is not { } box)
            return;
        if (box.ByRule)
        {
            AddRuleFrom(box);
            return;
        }
        if (box.Form.SelectedSubcategory is null)
        {
            box.Form.Error = Texts.Refusal(RefusalKind.UnknownTarget);
            return;
        }
        box.Form.Error = null;
        if (_hideFingerprintNote)
            MakeHandChoice(box);
        else
            box.ShowsPrivacy = true;
    }

    /// <summary>"Zaradiť" in the box about fingerprints: makes the hand choice, and keeps a tick in "Túto správu už nezobrazovať".</summary>
    [RelayCommand]
    private void ConfirmPrivacy()
    {
        if (Categorizing is not { ShowsPrivacy: true } box)
            return;
        if (box.HideNoteNextTime)
        {
            _hideFingerprintNote = true;
            _settings?.SaveHideFingerprintNote(true);
        }
        MakeHandChoice(box);
    }

    /// <summary>"Späť", or Escape: back to the "Zaradiť" box as it was; the tick isn't kept.</summary>
    [RelayCommand]
    private void BackFromPrivacy()
    {
        if (Categorizing is not { } box)
            return;
        box.ShowsPrivacy = false;
        box.HideNoteNextTime = false;
    }

    /// <summary>
    /// Escape closes the box on top: the box about fingerprints, the "Zaradiť" box, or the deletion question. With none
    /// open, it folds the open row of the transactions list (work/045-transaction-details/spec.md).
    /// </summary>
    [RelayCommand]
    private void Escape()
    {
        if (Categorizing is { ShowsPrivacy: true })
            BackFromPrivacy();
        else if (Categorizing is not null)
            CancelCategorize();
        else if (RuleToDelete is not null)
            TakeBackQuestion();
        else
            Statement.SelectedTransaction = null;
    }

    /// <summary>"× Zrušiť zaradenie": the transaction goes back to where the rules put it, without asking.</summary>
    [RelayCommand]
    private void UndoHandChoice(TransactionRow row)
    {
        if (!row.ByHand || row.Fingerprint is not { } print)
            return;
        var saved = ApplyUserChange(Statement.Config.RemoveHandChoice(print));
        var placement = Statement.Categorized!.Transactions[row.Index].Placement;
        ReportOnList(saved, Texts.HandChoiceUndone(Describe(placement)), Texts.UndoOnlyThisSession, Texts.UndoSaveFailed);
    }

    private void MakeHandChoice(CategorizeViewModel box)
    {
        var target = box.Form.SelectedSubcategory!.Id;
        var config = Statement.Config.WithFingerprintKey(Statement.Config.FingerprintKey ?? FingerprintKey.Create());
        var print = Fingerprints.Of(Statement.Categorized!.Statement, config.FingerprintKey!)[box.Row.Index];
        var outcome = config.SetHandChoice(print, target);
        if (!outcome.IsSuccess)
        {
            box.ShowsPrivacy = false;
            box.Form.Error = Texts.Refusal(outcome.Refusal!.Kind);
            return;
        }
        Categorizing = null;
        var saved = ApplyUserChange(outcome.Value!);
        var placement = Statement.Categorized!.Transactions[box.Row.Index].Placement;
        ReportOnList(saved, Texts.HandChosen(Describe(placement)), Texts.HandChoiceOnlyThisSession, Texts.HandChoiceSaveFailed);
    }

    /// <summary>
    /// Adds the box's rule on top, as "Pridať pravidlo" does. A rule made from a transaction must catch it, and takes
    /// over from that transaction's own hand choice, so the user sees the rule decide it.
    /// </summary>
    private void AddRuleFrom(CategorizeViewModel box)
    {
        if (box.Form.TryBuild() is not { } draft)
            return;
        if (!draft.Condition.Matches(box.Row.Transaction))
        {
            box.Form.Error = Texts.ConditionMissesTransaction;
            return;
        }
        var outcome = Statement.Config.AddUserRuleOnTop(draft.Condition, draft.Target);
        if (!outcome.IsSuccess)
        {
            box.Form.Error = Texts.Refusal(outcome.Refusal!.Kind);
            return;
        }
        var config = box.Row.Fingerprint is { } print ? outcome.Value!.RemoveHandChoice(print) : outcome.Value!;
        Categorizing = null;
        var saved = ApplyUserChange(config);
        var rule = Statement.Config.UserRules[^1].Id;
        var matched = Statement.Categorized?.Transactions.Count(t => t.Rule?.Id == rule) ?? 0;
        ReportOnList(saved, Texts.RuleMatched(matched), Texts.RuleOnlyThisSession, Texts.RuleSaveFailed);
    }

    /// <summary>The transactions card's answer to a change made from the list: the confirmation, or why it won't last.</summary>
    private void ReportOnList(bool saved, string confirmation, string onlyThisSession, string saveFailed)
    {
        Statement.TransactionsMessage = confirmation;
        Statement.TransactionsError = saved ? null : RulesNotSaving ? onlyThisSession : saveFailed;
    }

    /// <summary>"Potraviny › Supermarkety", or an Uncategorized bucket's name.</summary>
    private string Describe(Placement placement)
    {
        var (category, subcategory) = Statement.Config.Catalog.Describe(placement);
        return subcategory is null ? category.Name : $"{category.Name} › {subcategory.Name}";
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
        RulesListMessage = RulesListError = null;
        Statement.ClearTransactionsMessage();
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
