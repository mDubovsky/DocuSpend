using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;

namespace DocuSpend.Presentation.Tests;

/// <summary>
/// The rule editor: the list in the order tried, editing, deleting and the built-in rules
/// (work/024-edit-rules/spec.md). The window's part is in DocuSpend.Desktop.Tests.
/// </summary>
public sealed class RuleEditorTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    private string RulesFile => Path.Combine(_folder, "rules.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private static readonly SubcategoryId Games = new("leisure.games");
    private static readonly SubcategoryId Rent = new("housing.rent");

    /// <summary>Saves the rules as given, in creation order, and starts DocuSpend on them with a sample open.</summary>
    private async Task<MainViewModel> StartWith(params (Condition Condition, SubcategoryId Target, int Priority)[] rules)
    {
        var config = CategorizationConfig.Default;
        foreach (var (condition, target, priority) in rules)
            config = config.AddUserRule(condition, target, priority).Value!;
        if (rules.Length > 0)
            Assert.True(new RuleStore(RulesFile).Save(config));
        return await Restart();
    }

    private async Task<MainViewModel> Restart(string sample = "01-typical-month")
    {
        var main = new MainViewModel(new RuleStore(RulesFile));
        await main.OpenFileAsync(Samples.Path(sample));
        Assert.True(main.HasStatement);
        return main;
    }

    private static Condition Text(string text) => new TextCondition(text, TextMode.Contains);

    private static void Target(RuleFormViewModel form, SubcategoryId target)
    {
        form.SelectedCategory = form.Categories.Single(c => c.Subcategories.Any(s => s.Id == target));
        form.SelectedSubcategory = form.Subcategories.Single(s => s.Id == target);
    }

    // The list

    [Fact]
    public async Task The_rules_are_listed_in_the_order_they_are_tried_with_their_numbers()
    {
        var main = await StartWith((Text("prvé"), Games, 3), (Text("druhé"), Games, 1), (Text("tretie"), Games, 1));

        Assert.Equal(
            [("1", "Text obsahuje „tretie“"), ("1", "Text obsahuje „druhé“"), ("3", "Text obsahuje „prvé“")],
            main.Rules.Select(r => (r.Priority, r.Condition)));
        Assert.Equal(
            main.Statement.Config.RulesInOrder().Where(r => r.Source == RuleSource.User).Select(r => r.Id),
            main.Rules.Select(r => r.Id));
    }

    [Fact]
    public async Task A_rule_added_through_the_form_is_listed_first()
    {
        var main = await StartWith((Text("prvé"), Games, 3), (Text("druhé"), Games, 2));
        main.RuleForm.Text = "nové";
        Target(main.RuleForm, Rent);

        main.AddRuleCommand.Execute(null);

        Assert.Equal(("2", "Text obsahuje „nové“"), (main.Rules[0].Priority, main.Rules[0].Condition));
        Assert.Equal(3, main.Rules.Count);
    }

    // Editing

    private static RuleRow Row(MainViewModel main, string condition) => main.Rules.Single(r => r.Condition == condition);

    private static (string Description, int Count) AnyCardPayment(MainViewModel main)
    {
        var description = main.Statement.Categorized!.Transactions
            .First(t => t.Transaction.Type == Statements.Model.TransactionType.CardPayment).Transaction.Description!;
        return (description, main.Statement.Categorized!.Transactions.Count(t => t.Transaction.Description == description));
    }

    [Fact]
    public async Task Upravit_opens_the_rule_in_the_form_with_its_values_and_number()
    {
        var main = await StartWith((new TextCondition("Kino", TextMode.EqualTo), Games, 4));

        main.EditRuleCommand.Execute(main.Rules[0]);

        var form = main.RuleForm;
        Assert.True(form.IsEditing);
        Assert.Equal("Upraviť pravidlo", form.Heading);
        Assert.Equal(ConditionKind.Text, form.SelectedKind);
        Assert.Equal(TextMode.EqualTo, form.SelectedTextMode.Mode);
        Assert.Equal("Kino", form.Text);
        Assert.Equal(Games, form.SelectedSubcategory?.Id);
        Assert.Equal("4", form.Priority);
        Assert.True(main.Rules[0].IsEditing);
    }

    [Theory]
    [MemberData(nameof(EveryKindTests.Cases), MemberType = typeof(EveryKindTests))]
    public async Task A_saved_rule_of_each_kind_shows_exactly_and_saving_it_unchanged_changes_nothing(EveryKindTests.Case @case)
    {
        var start = await StartWith((@case.Expected, Games, 7));
        var before = start.Statement.Config.UserRules.Single();

        start.EditRuleCommand.Execute(start.Rules[0]);
        var form = start.RuleForm;
        var shown = new RuleFormViewModel(start.Statement.Config.Catalog);
        @case.Fill(shown);
        Assert.Equal(shown.SelectedKind, form.SelectedKind);
        Assert.Equal((shown.Text, shown.Account, shown.Symbol), (form.Text, form.Account, form.Symbol));
        Assert.Equal(shown.Types.Select(t => t.IsChecked), form.Types.Select(t => t.IsChecked));
        if (form.IsText)
            Assert.Equal(shown.SelectedTextMode, form.SelectedTextMode);
        if (form.IsAmount)
            Assert.Equal(@case.Expected, new AmountCondition(Parse(form.AmountFrom), Parse(form.AmountTo)));

        start.SaveRuleCommand.Execute(null);

        Assert.Null(form.Error);
        Assert.Equal(before, start.Statement.Config.UserRules.Single());
        Assert.Equal(before, (await Restart()).Statement.Config.UserRules.Single());

        static decimal? Parse(string text) => text == "" ? null : SlovakFormat.TryParseAmount(text, out var value) ? value : throw new FormatException(text);
    }

    [Fact]
    public async Task A_changed_rule_categorizes_as_if_it_had_been_added_so_and_says_what_it_catches()
    {
        var main = await StartWith((Text("nič také"), Games, 2), (Text("ani toto"), Games, 2));
        var (description, count) = AnyCardPayment(main);
        var first = main.Statement.Config.UserRules[0];

        main.EditRuleCommand.Execute(Row(main, "Text obsahuje „nič také“"));
        main.RuleForm.SelectedTextMode = main.RuleForm.TextModes.Single(m => m.Mode == TextMode.EqualTo);
        main.RuleForm.Text = description;
        Target(main.RuleForm, Rent);
        main.SaveRuleCommand.Execute(null);

        var changed = main.Statement.Config.UserRules.Single(r => r.Id == first.Id);
        Assert.Equal(first with { Condition = new TextCondition(description, TextMode.EqualTo), Target = Rent }, changed);
        Assert.Equal(
            Categorizer.Categorize(Samples.Read("01-typical-month"), main.Statement.Config).Transactions.Select(t => (t.Placement, t.Rule)),
            main.Statement.Categorized!.Transactions.Select(t => (t.Placement, t.Rule)));
        Assert.All(main.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(Rent, t.Placement.Subcategory));
        Assert.Equal(Texts.RuleMatched(count), main.RuleForm.Message);
        Assert.Null(main.RuleForm.Error);
        Assert.False(main.RuleForm.IsEditing);
        Assert.Equal("Nové pravidlo", main.RuleForm.Heading);
        Assert.Equal("", main.RuleForm.Text);
        Assert.Null(main.RuleForm.SelectedSubcategory);
    }

    [Theory]
    [MemberData(nameof(EveryKindTests.Cases), MemberType = typeof(EveryKindTests))]
    public async Task A_text_rule_can_be_changed_to_any_kind(EveryKindTests.Case @case)
    {
        var main = await StartWith((Text("nič také"), Games, 3));
        var id = main.Rules[0].Id;

        main.EditRuleCommand.Execute(main.Rules[0]);
        main.RuleForm.Clear();
        @case.Fill(main.RuleForm);
        main.SaveRuleCommand.Execute(null);

        Assert.Null(main.RuleForm.Error);
        var rule = main.Statement.Config.UserRules.Single();
        Assert.Equal((id, @case.Expected, Games, 3), (rule.Id, rule.Condition, rule.Target, rule.Priority));
        Assert.Equal(@case.Description, main.Rules[0].Condition);
    }

    public static TheoryData<string, Action<RuleFormViewModel>, string> InvalidEdits => new()
    {
        { "empty text", f => f.Text = " ", Texts.Refusal(RefusalKind.EmptyText) },
        { "amount not a number", f => { f.SelectedKind = ConditionKind.Amount; f.AmountFrom = "dvanásť"; }, Texts.AmountNotANumber },
        { "upside-down range", f => { f.SelectedKind = ConditionKind.Amount; f.AmountFrom = "30"; f.AmountTo = "20"; }, Texts.Refusal(RefusalKind.InvalidAmountRange) },
        { "no type", f => f.SelectedKind = ConditionKind.Type, Texts.Refusal(RefusalKind.NoTypeChosen) },
        { "no sub-category", f => f.SelectedSubcategory = null, Texts.Refusal(RefusalKind.UnknownTarget) },
        { "priority 0", f => f.Priority = "0", Texts.Refusal(RefusalKind.InvalidPriority) },
        { "priority -1", f => f.Priority = "-1", Texts.Refusal(RefusalKind.InvalidPriority) },
        { "priority 1,5", f => f.Priority = "1,5", Texts.Refusal(RefusalKind.InvalidPriority) },
        { "priority abc", f => f.Priority = "abc", Texts.Refusal(RefusalKind.InvalidPriority) },
        { "priority empty", f => f.Priority = "", Texts.Refusal(RefusalKind.InvalidPriority) },
        { "priority 256", f => f.Priority = "256", Texts.Refusal(RefusalKind.InvalidPriority) },
    };

    [Theory]
    [MemberData(nameof(InvalidEdits))]
    public async Task An_invalid_edit_says_why_and_changes_nothing(string name, Action<RuleFormViewModel> spoil, string reason)
    {
        _ = name;
        var main = await StartWith((Text("kino"), Games, 3));
        var before = (main.Statement.Config.UserRules.Single(), main.Statement.Categorized!.Transactions.Select(t => t.Placement).ToList(), File.ReadAllText(RulesFile));

        main.EditRuleCommand.Execute(main.Rules[0]);
        spoil(main.RuleForm);
        main.SaveRuleCommand.Execute(null);

        Assert.Equal(reason, main.RuleForm.Error);
        Assert.Equal("Priorita musí byť celé číslo od 1 do 255.", Texts.Refusal(RefusalKind.InvalidPriority));
        Assert.True(main.RuleForm.IsEditing);
        Assert.Equal(before.Item1, main.Statement.Config.UserRules.Single());
        Assert.Equal(before.Item2, main.Statement.Categorized!.Transactions.Select(t => t.Placement));
        Assert.Equal(before.Item3, File.ReadAllText(RulesFile));
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("255", 255)]
    [InlineData(" 2 ", 2)]
    public async Task Priorities_from_1_to_255_are_accepted(string typed, int number)
    {
        var main = await StartWith((Text("kino"), Games, 3));

        main.EditRuleCommand.Execute(main.Rules[0]);
        main.RuleForm.Priority = typed;
        main.SaveRuleCommand.Execute(null);

        Assert.Null(main.RuleForm.Error);
        Assert.Equal(number, main.Statement.Config.UserRules.Single().Priority);
    }

    [Fact]
    public async Task A_renumbered_rule_moves_to_its_place_and_the_categorization_follows()
    {
        var main = await StartWith((Text("alfa"), Games, 1), (Text("beta"), Games, 1), (Text("gama"), Rent, 3));
        var (description, _) = AnyCardPayment(main);
        main.EditRuleCommand.Execute(Row(main, "Text obsahuje „gama“"));
        main.RuleForm.Text = description;
        main.SaveRuleCommand.Execute(null);
        main.EditRuleCommand.Execute(Row(main, "Text obsahuje „alfa“"));
        main.RuleForm.Text = description;
        main.SaveRuleCommand.Execute(null);
        Assert.All(main.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(Games, t.Placement.Subcategory));

        main.EditRuleCommand.Execute(main.Rules.Single(r => r.Priority == "3"));
        main.RuleForm.Priority = "1";
        main.SaveRuleCommand.Execute(null);

        // Among the rules numbered 1 the list goes newest first by creation: gama (3rd), beta (2nd), alfa (1st).
        Assert.Equal(["1", "1", "1"], main.Rules.Select(r => r.Priority));
        Assert.Equal(main.Statement.Config.UserRules[2].Id, main.Rules[0].Id);
        Assert.Equal(main.Statement.Config.UserRules[0].Id, main.Rules[2].Id);
        Assert.All(main.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(Rent, t.Placement.Subcategory));
    }

    [Fact]
    public async Task Of_two_rules_with_the_same_number_the_newer_still_wins_after_either_is_edited()
    {
        var main = await StartWith((Text("nič"), Games, 1), (Text("nič"), Rent, 1));
        var (description, _) = AnyCardPayment(main);

        foreach (var condition in new[] { "Text obsahuje „nič“", "Text obsahuje „nič“" })
        {
            main.EditRuleCommand.Execute(main.Rules.First(r => r.Condition == condition));
            main.RuleForm.Text = description;
            main.SaveRuleCommand.Execute(null);
        }

        Assert.All(main.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(Rent, t.Placement.Subcategory));
    }

    [Fact]
    public async Task Zrusit_changes_nothing_and_empties_the_form()
    {
        var main = await StartWith((Text("kino"), Games, 3));
        var before = main.Statement.Config.UserRules.Single();

        main.EditRuleCommand.Execute(main.Rules[0]);
        main.RuleForm.Text = "iné";
        main.CancelEditCommand.Execute(null);

        Assert.Equal(before, main.Statement.Config.UserRules.Single());
        Assert.False(main.RuleForm.IsEditing);
        Assert.Equal(("", null), (main.RuleForm.Text, main.RuleForm.SelectedSubcategory));
        Assert.False(main.Rules[0].IsEditing);
    }

    [Fact]
    public async Task An_edited_rule_is_back_as_edited_after_a_restart_and_was_saved_before_the_confirmation()
    {
        var main = await StartWith((Text("kino"), Games, 3));
        main.RuleForm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(RuleFormViewModel.Message) && main.RuleForm.Message is not null)
                Assert.Contains("Lumière", File.ReadAllText(RulesFile));
        };

        main.EditRuleCommand.Execute(main.Rules[0]);
        main.RuleForm.Text = "Lumière";
        main.RuleForm.Priority = "9";
        main.SaveRuleCommand.Execute(null);

        var restarted = await Restart();
        Assert.Equal(main.Statement.Config.UserRules, restarted.Statement.Config.UserRules);
        Assert.Equal(("9", "Text obsahuje „Lumière“"), (restarted.Rules[0].Priority, restarted.Rules[0].Condition));
    }

    [Fact]
    public async Task An_edit_while_saving_is_off_applies_and_says_it_lasts_until_DocuSpend_closes()
    {
        Directory.CreateDirectory(_folder);
        const string Newer = """{ "version": 2 }""";
        File.WriteAllText(RulesFile, Newer);
        var main = await Restart();
        main.RuleForm.Text = "kino";
        Target(main.RuleForm, Games);
        main.AddRuleCommand.Execute(null);

        main.EditRuleCommand.Execute(main.Rules[0]);
        main.RuleForm.Text = "nič také tu nie je";
        main.SaveRuleCommand.Execute(null);

        Assert.Equal(new TextCondition("nič také tu nie je", TextMode.Contains), main.Statement.Config.UserRules.Single().Condition);
        Assert.Equal(Texts.RuleMatchedNothingUnsaved, main.RuleForm.Message);
        Assert.Equal("Zmena platí len do zatvorenia DocuSpend.", main.RuleForm.Error);
        Assert.Equal(Newer, File.ReadAllText(RulesFile));
    }

    [Fact]
    public async Task An_edit_that_cannot_be_saved_applies_says_so_and_the_next_change_saves_it()
    {
        var folder = Path.Combine(_folder, "DocuSpend");
        Directory.CreateDirectory(_folder);
        var file = Path.Combine(folder, "rules.json");
        var main = new MainViewModel(new RuleStore(file));
        await main.OpenFileAsync(Samples.Path("01-typical-month"));
        main.RuleForm.Text = "kino";
        Target(main.RuleForm, Games);
        main.AddRuleCommand.Execute(null);
        File.WriteAllText(folder + ".tmp", "");
        Directory.Delete(folder, recursive: true);
        File.Move(folder + ".tmp", folder);

        main.EditRuleCommand.Execute(main.Rules[0]);
        main.RuleForm.Text = "Lumière";
        main.SaveRuleCommand.Execute(null);

        Assert.Equal("Zmenu sa nepodarilo uložiť, preto po zatvorení DocuSpend neostane. Pri ďalšej zmene sa DocuSpend pokúsi uložiť znova.",
            main.RuleForm.Error);
        Assert.Equal(new TextCondition("Lumière", TextMode.Contains), main.Statement.Config.UserRules.Single().Condition);

        File.Delete(folder);
        main.EditRuleCommand.Execute(main.Rules[0]);
        main.RuleForm.Priority = "5";
        main.SaveRuleCommand.Execute(null);

        Assert.Null(main.RuleForm.Error);
        var restarted = new MainViewModel(new RuleStore(file));
        Assert.Equal(("5", "Text obsahuje „Lumière“"), (restarted.Rules[0].Priority, restarted.Rules[0].Condition));
    }

    [Fact]
    public async Task A_file_saved_after_edits_has_the_first_shape()
    {
        var main = await StartWith((Text("kino"), Games, 3));
        main.EditRuleCommand.Execute(main.Rules[0]);
        main.RuleForm.Priority = "8";
        main.SaveRuleCommand.Execute(null);

        var reading = Categorization.Saving.SavedRules.Read(File.ReadAllText(RulesFile), CategorizationConfig.Default);
        Assert.Equal(Categorization.Saving.SavedRulesStatus.Loaded, reading.Status);
        Assert.Contains("\"version\": 1", File.ReadAllText(RulesFile));
    }

    // Editing: edge cases

    [Fact]
    public async Task Upravit_replaces_a_half_typed_new_rule_and_Zrusit_then_leaves_the_form_empty()
    {
        var main = await StartWith((Text("kino"), Games, 3));
        main.RuleForm.SelectedKind = ConditionKind.Amount;
        main.RuleForm.AmountFrom = "12";

        main.EditRuleCommand.Execute(main.Rules[0]);
        Assert.Equal((ConditionKind.Text, "", "kino"), (main.RuleForm.SelectedKind, main.RuleForm.AmountFrom, main.RuleForm.Text));

        main.CancelEditCommand.Execute(null);
        Assert.Equal((ConditionKind.Text, "", ""), (main.RuleForm.SelectedKind, main.RuleForm.AmountFrom, main.RuleForm.Text));
    }

    [Fact]
    public async Task Upravit_on_another_rule_switches_to_it_and_drops_the_unsaved_changes()
    {
        var main = await StartWith((Text("alfa"), Games, 2), (Text("beta"), Rent, 1));

        main.EditRuleCommand.Execute(Row(main, "Text obsahuje „alfa“"));
        main.RuleForm.Text = "zmenené";
        main.EditRuleCommand.Execute(Row(main, "Text obsahuje „beta“"));
        main.SaveRuleCommand.Execute(null);

        Assert.Equal(["Text obsahuje „beta“", "Text obsahuje „alfa“"], main.Rules.Select(r => r.Condition));
        Assert.Equal(Rent, main.Statement.Config.UserRules[1].Target);
    }

    [Fact]
    public async Task Opening_another_statement_while_editing_keeps_the_form_and_saving_applies_to_the_new_one()
    {
        var main = await StartWith((Text("nič také tu nie je"), Games, 3));
        main.EditRuleCommand.Execute(main.Rules[0]);
        await main.OpenFileAsync(Samples.Path("02-busy-month"));
        Assert.True(main.RuleForm.IsEditing);
        var (description, _) = AnyCardPayment(main);

        main.RuleForm.Text = description;
        main.SaveRuleCommand.Execute(null);

        Assert.All(main.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(Games, t.Placement.Subcategory));
    }

    [Fact]
    public async Task An_edit_can_make_two_rules_identical_and_then_the_newer_wins()
    {
        var main = await StartWith((Text("kino"), Games, 1), (Text("divadlo"), Rent, 1));

        main.EditRuleCommand.Execute(Row(main, "Text obsahuje „divadlo“"));
        main.RuleForm.Text = "kino";
        Target(main.RuleForm, Games);
        main.SaveRuleCommand.Execute(null);

        Assert.Null(main.RuleForm.Error);
        Assert.Equal(2, main.Rules.Count(r => r.Condition == "Text obsahuje „kino“"));
    }

    [Fact]
    public async Task A_saved_rule_numbered_above_255_is_listed_with_its_number_and_needs_1_to_255_to_be_saved()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(RulesFile, """
            { "version": 1, "categories": [], "subcategories": [],
              "rules": [ { "id": "user-a0000000000000000000000000000001", "priority": 300, "order": 1, "target": "leisure.games",
                           "condition": { "kind": "text", "text": "kino", "mode": "contains" } } ] }
            """);
        var main = await Restart();
        Assert.Equal("300", main.Rules.Single().Priority);

        main.EditRuleCommand.Execute(main.Rules[0]);
        main.SaveRuleCommand.Execute(null);
        Assert.Equal(Texts.Refusal(RefusalKind.InvalidPriority), main.RuleForm.Error);

        main.RuleForm.Priority = "255";
        main.SaveRuleCommand.Execute(null);
        Assert.Equal("255", main.Rules.Single().Priority);
    }

    // Deleting

    [Fact]
    public async Task Odstranit_only_asks_first()
    {
        var main = await StartWith((Text("alfa"), Games, 2), (Text("beta"), Rent, 1));

        main.AskToDeleteCommand.Execute(Row(main, "Text obsahuje „alfa“"));

        Assert.Equal(2, main.Statement.Config.UserRules.Count);
        Assert.Equal([false, true], main.Rules.Select(r => r.IsAskingToDelete));
        Assert.True(main.KeepRuleCommand.CanExecute(null));
        Assert.Equal("Odstrániť toto pravidlo?", Texts.DeleteQuestion);
    }

    [Fact]
    public async Task Only_one_row_asks_at_a_time()
    {
        var main = await StartWith((Text("alfa"), Games, 2), (Text("beta"), Rent, 1));

        main.AskToDeleteCommand.Execute(main.Rules[0]);
        main.AskToDeleteCommand.Execute(main.Rules[1]);

        Assert.Equal([false, true], main.Rules.Select(r => r.IsAskingToDelete));
    }

    public static TheoryData<string> OtherActions => new(["keep", "add", "edit", "cancel"]);

    [Theory]
    [MemberData(nameof(OtherActions))]
    public async Task Keeping_or_starting_anything_else_takes_the_question_back(string action)
    {
        var main = await StartWith((Text("alfa"), Games, 2), (Text("beta"), Rent, 1));
        main.AskToDeleteCommand.Execute(main.Rules[0]);

        switch (action)
        {
            case "keep": main.KeepRuleCommand.Execute(null); break;
            case "add": main.RuleForm.Text = "gama"; Target(main.RuleForm, Games); main.AddRuleCommand.Execute(null); break;
            case "edit": main.EditRuleCommand.Execute(main.Rules[1]); break;
            case "cancel": main.CancelEditCommand.Execute(null); break;
        }

        Assert.All(main.Rules, r => Assert.False(r.IsAskingToDelete));
        Assert.False(main.KeepRuleCommand.CanExecute(null));
        Assert.Contains(main.Rules, r => r.Condition == "Text obsahuje „beta“");
        main.DeleteRuleCommand.Execute(null);
        Assert.Contains(main.Rules, r => r.Condition == "Text obsahuje „beta“");
    }

    [Fact]
    public async Task A_deleted_rule_is_gone_the_statement_is_categorized_without_it_and_it_is_not_back_after_a_restart()
    {
        var main = await StartWith((Text("nič"), Games, 1));
        var (description, _) = AnyCardPayment(main);
        main.EditRuleCommand.Execute(main.Rules[0]);
        main.RuleForm.Text = description;
        main.SaveRuleCommand.Execute(null);
        var without = Categorizer.Categorize(Samples.Read("01-typical-month"), CategorizationConfig.Default);
        main.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.RulesListMessage) && main.RulesListMessage is not null)
                Assert.DoesNotContain("user-", File.ReadAllText(RulesFile));
        };

        main.AskToDeleteCommand.Execute(main.Rules[0]);
        main.DeleteRuleCommand.Execute(null);

        Assert.Empty(main.Rules);
        Assert.Equal(without.Transactions.Select(t => t.Placement), main.Statement.Categorized!.Transactions.Select(t => t.Placement));
        Assert.Equal("Pravidlo je odstránené.", main.RulesListMessage);
        Assert.Null(main.RulesListError);
        Assert.Empty((await Restart()).Rules);
    }

    [Fact]
    public async Task The_deletion_message_lasts_until_the_next_change()
    {
        var main = await StartWith((Text("alfa"), Games, 2), (Text("beta"), Rent, 1));
        main.AskToDeleteCommand.Execute(main.Rules[0]);
        main.DeleteRuleCommand.Execute(null);
        Assert.NotNull(main.RulesListMessage);

        main.EditRuleCommand.Execute(main.Rules[0]);
        Assert.NotNull(main.RulesListMessage);
        main.SaveRuleCommand.Execute(null);

        Assert.Null(main.RulesListMessage);
    }

    [Fact]
    public async Task Deleting_the_rule_open_in_the_form_empties_the_form()
    {
        var main = await StartWith((Text("alfa"), Games, 2));
        main.EditRuleCommand.Execute(main.Rules[0]);

        main.AskToDeleteCommand.Execute(main.Rules[0]);
        main.DeleteRuleCommand.Execute(null);

        Assert.False(main.RuleForm.IsEditing);
        Assert.Equal("", main.RuleForm.Text);
    }

    [Fact]
    public async Task After_the_last_rule_is_deleted_the_list_is_empty_and_the_next_rule_gets_number_1()
    {
        var main = await StartWith((Text("alfa"), Games, 7));
        main.AskToDeleteCommand.Execute(main.Rules[0]);
        main.DeleteRuleCommand.Execute(null);
        Assert.Empty(main.Rules);

        main.RuleForm.Text = "beta";
        Target(main.RuleForm, Games);
        main.AddRuleCommand.Execute(null);

        Assert.Equal("1", main.Rules.Single().Priority);
    }

    [Fact]
    public async Task A_deletion_while_saving_is_off_applies_and_says_the_rule_will_come_back()
    {
        Directory.CreateDirectory(_folder);
        const string Newer = """{ "version": 2 }""";
        File.WriteAllText(RulesFile, Newer);
        var main = await Restart();
        main.RuleForm.Text = "kino";
        Target(main.RuleForm, Games);
        main.AddRuleCommand.Execute(null);

        main.AskToDeleteCommand.Execute(main.Rules[0]);
        main.DeleteRuleCommand.Execute(null);

        Assert.Empty(main.Rules);
        Assert.Equal("Pravidlo je odstránené.", main.RulesListMessage);
        Assert.Equal("Pravidlo sa po zatvorení DocuSpend vráti.", main.RulesListError);
        Assert.Equal(Newer, File.ReadAllText(RulesFile));
    }

    [Fact]
    public async Task A_deletion_that_cannot_be_saved_applies_says_so_and_the_next_change_saves_it()
    {
        var folder = Path.Combine(_folder, "DocuSpend");
        var file = Path.Combine(folder, "rules.json");
        var config = CategorizationConfig.Default.AddUserRule(Text("alfa"), Games, 1).Value!.AddUserRule(Text("beta"), Rent, 1).Value!;
        Assert.True(new RuleStore(file).Save(config));
        var main = new MainViewModel(new RuleStore(file));
        await main.OpenFileAsync(Samples.Path("01-typical-month"));
        Directory.Delete(folder, recursive: true);
        File.WriteAllText(folder, "");

        main.AskToDeleteCommand.Execute(Row(main, "Text obsahuje „alfa“"));
        main.DeleteRuleCommand.Execute(null);

        Assert.Equal(["Text obsahuje „beta“"], main.Rules.Select(r => r.Condition));
        Assert.Equal("Odstránenie sa nepodarilo uložiť, preto sa pravidlo po zatvorení DocuSpend vráti. Pri ďalšej zmene sa DocuSpend pokúsi uložiť znova.",
            main.RulesListError);

        File.Delete(folder);
        main.EditRuleCommand.Execute(main.Rules[0]);
        main.SaveRuleCommand.Execute(null);

        Assert.Equal(["Text obsahuje „beta“"], new MainViewModel(new RuleStore(file)).Rules.Select(r => r.Condition));
    }

    // Built-in rules

    [Fact]
    public async Task Every_built_in_rule_is_listed_in_the_order_tried_with_256()
    {
        var main = await StartWith((Text("alfa"), Games, 2));

        var defaults = main.Statement.Config.RulesInOrder().Where(r => r.Source == RuleSource.Default).ToList();
        Assert.Equal(defaults.Select(r => r.Id), main.BuiltInRules.Select(r => r.Id));
        Assert.All(main.BuiltInRules, r => Assert.Equal("256", r.Priority));
        Assert.Equal(Texts.BuiltInPriority, (CategorizationConfig.MaxUserPriority + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(MainViewModel.Describe(defaults[0].Condition), main.BuiltInRules[0].Condition);
        Assert.Equal("Vstavané pravidlá", Texts.BuiltInRules);
    }

    [Fact]
    public async Task No_change_to_user_rules_changes_the_built_in_rules()
    {
        var main = await StartWith((Text("alfa"), Games, 2), (Text("beta"), Games, 3));
        var before = main.BuiltInRules.ToList();
        var defaults = main.Statement.Config.DefaultRules;

        main.EditRuleCommand.Execute(main.Rules[0]);
        main.RuleForm.Priority = "1";
        main.SaveRuleCommand.Execute(null);
        main.AskToDeleteCommand.Execute(main.Rules[1]);
        main.DeleteRuleCommand.Execute(null);

        Assert.Equal(before, main.BuiltInRules);
        Assert.Equal(defaults, main.Statement.Config.DefaultRules);
    }

    // Many rules

    [Fact]
    public async Task With_200_rules_an_edit_and_a_deletion_each_take_under_a_second()
    {
        var rules = Enumerable.Range(1, 200).Select(i => (Text($"obchod {i}"), Games, i % 20 + 1)).ToArray();
        var main = await StartWith(rules);
        Assert.Equal(200, main.Rules.Count);

        var clock = System.Diagnostics.Stopwatch.StartNew();
        main.EditRuleCommand.Execute(main.Rules[100]);
        main.RuleForm.Priority = "1";
        main.SaveRuleCommand.Execute(null);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(1), $"The edit took {clock.Elapsed}.");

        clock.Restart();
        main.AskToDeleteCommand.Execute(main.Rules[50]);
        main.DeleteRuleCommand.Execute(null);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(1), $"The deletion took {clock.Elapsed}.");
        Assert.Equal(199, main.Rules.Count);
    }

    [Fact]
    public void The_priority_help_says_what_the_numbers_mean() =>
        Assert.Equal(
            "Pravidlá sa uplatňujú od priority 1. Transakciu zaradí prvé pravidlo, ktorému zodpovedá. Vaše pravidlá môžu mať prioritu od 1 do 255; vstavané pravidlá majú prioritu 256, takže vaše pravidlá majú vždy prednosť. Ak majú dve pravidlá rovnakú prioritu, rozhoduje novšie.",
            Texts.PriorityHelp);
}
