using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;

namespace DocuSpend.Presentation.Tests;

/// <summary>
/// The window's state: adding rules that are saved and survive a restart, and the choice of look (spec: adding a
/// rule, rules survive a restart). Opening statements is in <see cref="OpeningTests"/>.
/// </summary>
public sealed class MainViewModelTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    private string RulesFile => Path.Combine(_folder, "rules.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    /// <summary>A fresh start of the application, with the rules file of this test, and a sample opened.</summary>
    private async Task<MainViewModel> Start(string sample = "01-typical-month")
    {
        var main = new MainViewModel(new RuleStore(RulesFile));
        await main.OpenFileAsync(Samples.Path(sample));
        Assert.True(main.HasStatement);
        return main;
    }

    private static void Target(RuleFormViewModel form, SubcategoryId target)
    {
        form.SelectedCategory = form.Categories.Single(c => c.Subcategories.Any(s => s.Id == target));
        form.SelectedSubcategory = form.Subcategories.Single(s => s.Id == target);
    }

    private static readonly SubcategoryId Games = new("leisure.games");

    /// <summary>Adds a text rule for one of the shown statement's uncategorized payments, and returns its description.</summary>
    private static string AddRuleForAnUncategorizedPayment(MainViewModel main)
    {
        main.Statement.SelectedCategory = new CategoryId("uncategorized-out");
        var description = main.Statement.Transactions[0].Transaction.Description!;
        main.RuleForm.SelectedKind = ConditionKind.Text;
        main.RuleForm.Text = description;
        main.RuleForm.SelectedTextMode = main.RuleForm.TextModes.Single(m => m.Mode == TextMode.EqualTo);
        Target(main.RuleForm, Games);
        main.AddRuleCommand.Execute(null);
        return description;
    }

    [Fact]
    public async Task An_added_rule_categorizes_again_at_once_and_is_listed()
    {
        var main = await Start();

        var description = AddRuleForAnUncategorizedPayment(main);

        Assert.Null(main.RuleForm.Error);
        Assert.Contains(main.Statement.Categories, c => c.Category == new CategoryId("leisure"));
        var placed = main.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description);
        Assert.All(placed, t => Assert.Equal(Games, t.Placement.Subcategory));
        var rule = Assert.Single(main.Rules);
        Assert.Contains(description, rule.Condition);
        Assert.Equal("Zábava a oddych › Hry a hračky", rule.Target);
        Assert.Equal(Texts.RuleMatched(placed.Count()), main.RuleForm.Message);
    }

    [Fact]
    public async Task An_added_rule_beats_every_earlier_rule()
    {
        var main = await Start();
        var description = AddRuleForAnUncategorizedPayment(main);

        main.RuleForm.SelectedKind = ConditionKind.Text;
        main.RuleForm.Text = description;
        Target(main.RuleForm, new SubcategoryId("food.groceries"));
        main.AddRuleCommand.Execute(null);

        Assert.All(main.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(new SubcategoryId("food.groceries"), t.Placement.Subcategory));
    }

    [Fact]
    public async Task A_rule_survives_a_restart()
    {
        var description = AddRuleForAnUncategorizedPayment(await Start());

        var restarted = await Start();

        Assert.Single(restarted.Rules);
        Assert.All(restarted.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(Games, t.Placement.Subcategory));
    }

    [Fact]
    public async Task Saved_rules_apply_to_every_statement_opened()
    {
        var main = await Start();
        main.RuleForm.SelectedKind = ConditionKind.Type;
        main.RuleForm.Types[0].IsChecked = true;
        Target(main.RuleForm, Games);
        main.AddRuleCommand.Execute(null);

        await main.OpenFileAsync(Samples.Path("02-busy-month"));

        var expected = Categorizer.Categorize(Samples.Read("02-busy-month"), main.Statement.Config);
        Assert.Equal(expected.Transactions.Select(t => t.Placement), main.Statement.Categorized!.Transactions.Select(t => t.Placement));
        Assert.Contains(main.Statement.Categorized!.Transactions, t => t.Placement.Subcategory == Games);
    }

    [Fact]
    public async Task Rules_saved_before_are_applied_to_a_statement_opened_after_a_restart()
    {
        var description = AddRuleForAnUncategorizedPayment(await Start());

        var restarted = new MainViewModel(new RuleStore(RulesFile));
        Assert.Single(restarted.Rules);
        await restarted.OpenFileAsync(Samples.Path("01-typical-month"));

        Assert.All(restarted.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(Games, t.Placement.Subcategory));
    }

    [Fact]
    public async Task A_rule_that_matches_nothing_is_saved_and_says_so()
    {
        var main = await Start();
        main.RuleForm.SelectedKind = ConditionKind.Text;
        main.RuleForm.Text = "nič také tu nie je";
        Target(main.RuleForm, Games);

        main.AddRuleCommand.Execute(null);

        Assert.Single(main.Rules);
        Assert.Equal(Texts.RuleMatchedNothing, main.RuleForm.Message);
        Assert.Single((await Start()).Rules);
    }

    [Fact]
    public async Task Invalid_input_saves_nothing()
    {
        var main = await Start();
        main.RuleForm.SelectedKind = ConditionKind.Text;
        Target(main.RuleForm, Games);

        main.AddRuleCommand.Execute(null);

        Assert.Equal(Texts.Refusal(RefusalKind.EmptyText), main.RuleForm.Error);
        Assert.Empty(main.Rules);
        Assert.False(File.Exists(RulesFile));
    }

    // Keeping the rules (spec: starting; when the saved rules can't be used; when a change can't be saved).

    private string[] FilesInFolder() =>
        Directory.EnumerateFileSystemEntries(_folder).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray()!;

    [Fact]
    public async Task Without_saved_rules_there_is_no_message_and_saving_is_on()
    {
        var main = await Start();

        Assert.Null(main.RulesMessage);
        Assert.False(main.RulesNotSaving);
        Assert.Empty(main.Rules);
        Assert.Same(CategorizationConfig.Default, main.Statement.Config);
    }

    [Fact]
    public async Task A_rule_is_saved_before_its_confirmation_is_shown()
    {
        var main = await Start();
        int? savedWhenConfirmed = null;
        main.RuleForm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(RuleFormViewModel.Message) && main.RuleForm.Message is not null)
                savedWhenConfirmed = new RuleStore(RulesFile).Load(CategorizationConfig.Default).Config.UserRules.Count;
        };

        AddRuleForAnUncategorizedPayment(main);

        Assert.Equal(1, savedWhenConfirmed);
    }

    [Fact]
    public async Task After_a_restart_the_rules_are_listed_in_the_same_order_and_categorize_the_same()
    {
        var main = await Start("02-busy-month");
        main.RuleForm.SelectedKind = ConditionKind.Type;
        main.RuleForm.Types[0].IsChecked = true;
        Target(main.RuleForm, Games);
        main.AddRuleCommand.Execute(null);
        main.RuleForm.SelectedKind = ConditionKind.Amount;
        main.RuleForm.AmountFrom = "20";
        main.RuleForm.AmountTo = "60,50";
        Target(main.RuleForm, new SubcategoryId("food.restaurants"));
        main.AddRuleCommand.Execute(null);
        AddRuleForAnUncategorizedPayment(main);

        var restarted = await Start("02-busy-month");

        Assert.Equal(3, restarted.Rules.Count);
        Assert.Equal(main.Rules, restarted.Rules);
        Assert.Equal(
            main.Statement.Categorized!.Transactions.Select(t => (t.Placement, t.Rule)),
            restarted.Statement.Categorized!.Transactions.Select(t => (t.Placement, t.Rule)));
    }

    [Fact]
    public async Task A_damaged_file_is_reported_with_where_it_was_moved_and_new_rules_are_saved()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(RulesFile, "{ damaged");

        var main = await Start();

        var moved = Assert.Single(FilesInFolder());
        Assert.StartsWith("rules.damaged-", moved);
        Assert.Equal(Texts.RulesDamaged(Path.Combine(_folder, moved)), main.RulesMessage);
        Assert.False(main.RulesNotSaving);
        Assert.Empty(main.Rules);

        AddRuleForAnUncategorizedPayment(main);
        Assert.Null(main.RuleForm.Error);
        Assert.Single((await Start()).Rules);
        Assert.Equal("{ damaged", File.ReadAllText(Path.Combine(_folder, moved)));
    }

    [Fact]
    public async Task The_damaged_file_message_contains_nothing_from_the_file()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(RulesFile, """{ "version": 1, "rules": [ "MINIMARKET U LIPY TRENCIN", "Katarína Vzorová" ] }""");

        var main = await Start();

        Assert.NotNull(main.RulesMessage);
        Assert.DoesNotContain("MINIMARKET", main.RulesMessage);
        Assert.DoesNotContain("Vzorová", main.RulesMessage);
        Assert.DoesNotContain("version", main.RulesMessage);
    }

    [Fact]
    public async Task A_file_that_cannot_be_opened_turns_saving_off_but_a_new_rule_still_applies()
    {
        AddRuleForAnUncategorizedPayment(await Start());
        var before = File.ReadAllBytes(RulesFile);

        MainViewModel main;
        using (new FileStream(RulesFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            main = await Start();

        Assert.Equal(Texts.RulesCannotOpen, main.RulesMessage);
        Assert.True(main.RulesNotSaving);
        Assert.Empty(main.Rules);

        var description = AddRuleForAnUncategorizedPayment(main);

        Assert.Single(main.Rules);
        Assert.All(main.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description),
            t => Assert.Equal(Games, t.Placement.Subcategory));
        Assert.StartsWith("Pravidlo zaradilo", main.RuleForm.Message);
        Assert.Equal(Texts.RuleOnlyThisSession, main.RuleForm.Error);
        Assert.Equal(before, File.ReadAllBytes(RulesFile));
    }

    [Fact]
    public async Task A_file_from_a_newer_DocuSpend_turns_saving_off_and_is_left_untouched()
    {
        Directory.CreateDirectory(_folder);
        const string Newer = """{ "version": 2, "rulePacks": [] }""";
        File.WriteAllText(RulesFile, Newer);

        var main = await Start();
        main.RuleForm.SelectedKind = ConditionKind.Text;
        main.RuleForm.Text = "nič také tu nie je";
        Target(main.RuleForm, Games);
        main.AddRuleCommand.Execute(null);

        Assert.Equal(Texts.RulesNewer, main.RulesMessage);
        Assert.True(main.RulesNotSaving);
        Assert.Equal(Texts.RuleMatchedNothingUnsaved, main.RuleForm.Message);
        Assert.Equal(Texts.RuleOnlyThisSession, main.RuleForm.Error);
        Assert.Equal(Newer, File.ReadAllText(RulesFile));
        Assert.Equal(["rules.json"], FilesInFolder());
    }

    [Fact]
    public async Task A_rule_that_cannot_be_saved_applies_and_says_so_and_the_next_save_keeps_both()
    {
        // A file where the folder should be: the folder can't be created, so nothing can be written.
        var folder = Path.Combine(_folder, "DocuSpend");
        Directory.CreateDirectory(_folder);
        File.WriteAllText(folder, "");
        var file = Path.Combine(folder, "rules.json");
        var main = new MainViewModel(new RuleStore(file));
        await main.OpenFileAsync(Samples.Path("01-typical-month"));

        AddRuleForAnUncategorizedPayment(main);

        Assert.Single(main.Rules);
        Assert.StartsWith("Pravidlo zaradilo", main.RuleForm.Message);
        Assert.Equal(Texts.RuleSaveFailed, main.RuleForm.Error);
        Assert.False(main.RulesNotSaving);

        File.Delete(folder);
        main.RuleForm.SelectedKind = ConditionKind.Amount;
        main.RuleForm.AmountFrom = "1000";
        Target(main.RuleForm, Games);
        main.AddRuleCommand.Execute(null);

        Assert.Null(main.RuleForm.Error);
        Assert.Equal(2, new MainViewModel(new RuleStore(file)).Rules.Count);
    }

    [Fact]
    public async Task The_rules_message_can_be_closed_and_shows_beside_a_message_about_a_statement()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(RulesFile, "{ damaged");
        var main = await Start();

        await main.OpenFileAsync(Samples.Path("07-not-a-statement"));

        Assert.NotNull(main.OpenMessage);
        Assert.NotNull(main.RulesMessage);
        main.CloseRulesMessageCommand.Execute(null);
        Assert.Null(main.RulesMessage);
        Assert.NotNull(main.OpenMessage);
    }

    [Fact]
    public void The_rules_message_shows_on_the_start_screen_before_any_statement()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(RulesFile, "{ damaged");

        var main = new MainViewModel(new RuleStore(RulesFile));

        Assert.False(main.HasStatement);
        Assert.NotNull(main.RulesMessage);
    }

    [Fact]
    public async Task The_spikes_rules_file_is_ignored_and_left_as_it_is()
    {
        Directory.CreateDirectory(_folder);
        var spike = Path.Combine(_folder, "spike-rules.json");
        const string Old = """{ "version": 1, "rules": [ { "kind": "text", "text": "MINIMARKET", "mode": "Contains", "target": "leisure.games" } ] }""";
        File.WriteAllText(spike, Old);

        var main = await Start();

        Assert.Empty(main.Rules);
        Assert.Null(main.RulesMessage);
        Assert.Equal(Old, File.ReadAllText(spike));
    }

    [Fact]
    public void Every_kind_of_rule_is_described_in_slovak()
    {
        Assert.Equal("Protistrana obsahuje „vzor“", MainViewModel.Describe(new CounterpartyNameCondition("vzor", TextMode.Contains)));
        Assert.Equal("Protistrana sa rovná „Pavol Vzor“", MainViewModel.Describe(new CounterpartyNameCondition("Pavol Vzor", TextMode.EqualTo)));
        Assert.Equal("Účet protistrany: SK57 1100", MainViewModel.Describe(new CounterpartyAccountCondition("SK57 1100")));
        Assert.Equal("Variabilný symbol: 0326", MainViewModel.Describe(new VariableSymbolCondition("0326")));
        Assert.Equal("Typ: Prevod, Okamžitý prevod", MainViewModel.Describe(
            new TypeCondition([Statements.Model.TransactionType.InstantTransfer, Statements.Model.TransactionType.Transfer])));
    }

    [Fact]
    public void Rules_of_every_kind_loaded_from_the_file_are_listed()
    {
        var config = CategorizationConfig.Default;
        config = config.AddUserRule(new CounterpartyNameCondition("vzor", TextMode.Contains), Games, 1).Value!;
        config = config.AddUserRule(new CounterpartyAccountCondition("SK57 1100"), Games, 1).Value!;
        config = config.AddUserRule(new VariableSymbolCondition("0326"), Games, 1).Value!;
        Assert.True(new RuleStore(RulesFile).Save(config));

        var main = new MainViewModel(new RuleStore(RulesFile));

        Assert.Equal(["Protistrana obsahuje „vzor“", "Účet protistrany: SK57 1100", "Variabilný symbol: 0326"], main.Rules.Select(r => r.Condition));
    }

    [Fact]
    public void Rules_are_described_in_slovak()
    {
        Assert.Equal("Text obsahuje „netflix“", MainViewModel.Describe(new TextCondition("netflix", TextMode.Contains)));
        Assert.Equal("Text sa rovná „netflix“", MainViewModel.Describe(new TextCondition("netflix", TextMode.EqualTo)));
        Assert.Equal("Typ: Okamžitý prevod",
            MainViewModel.Describe(new TypeCondition([Statements.Model.TransactionType.InstantTransfer])));
        Assert.Equal("Suma od 10,00 € do 20,00 €", MainViewModel.Describe(new AmountCondition(10m, 20m)).Replace(' ', ' '));
        Assert.Equal("Suma od 10,00 €", MainViewModel.Describe(new AmountCondition(10m, null)).Replace(' ', ' '));
        Assert.Equal("Suma do 20,00 €", MainViewModel.Describe(new AmountCondition(null, 20m)).Replace(' ', ' '));
    }

    [Fact]
    public void The_look_offers_three_choices_in_slovak_and_follows_windows_at_first()
    {
        var main = new MainViewModel(new RuleStore(RulesFile));

        Assert.Equal([Look.LookChoice.System, Look.LookChoice.Light, Look.LookChoice.Dark], main.Looks.Select(l => l.Choice));
        Assert.Equal(["Podľa systému", "Svetlý", "Tmavý"], main.Looks.Select(l => l.Label));
        Assert.Equal(Look.LookChoice.System, main.Look);
    }

    [Fact]
    public void A_chosen_look_is_saved_and_in_effect_after_a_restart()
    {
        var settings = Path.Combine(_folder, "settings.json");
        var main = new MainViewModel(new RuleStore(RulesFile), new Look.SettingsStore(settings));

        main.SelectedLook = main.Looks.Single(l => l.Choice == Look.LookChoice.Dark);

        var restarted = new MainViewModel(new RuleStore(RulesFile), new Look.SettingsStore(settings));
        Assert.Equal(Look.LookChoice.Dark, restarted.Look);
        Assert.Equal(Look.LookChoice.Dark, restarted.SelectedLook.Choice);
    }

    [Fact]
    public async Task Changing_the_look_keeps_what_the_user_was_doing()
    {
        var main = await Start();
        main.Statement.SelectedCategory = new CategoryId("housing");
        main.RuleForm.Text = "rozpísané";

        main.SelectedLook = main.Looks.Single(l => l.Choice == Look.LookChoice.Light);

        Assert.Equal(new CategoryId("housing"), main.Statement.SelectedCategory);
        Assert.Equal("rozpísané", main.RuleForm.Text);
    }

    [Theory]
    [InlineData(1, "Pravidlo zaradilo 1 transakciu.")]
    [InlineData(3, "Pravidlo zaradilo 3 transakcie.")]
    [InlineData(5, "Pravidlo zaradilo 5 transakcií.")]
    public void The_count_of_matched_transactions_is_in_correct_slovak(int count, string expected)
    {
        Assert.Equal(expected, Texts.RuleMatched(count));
    }
}
