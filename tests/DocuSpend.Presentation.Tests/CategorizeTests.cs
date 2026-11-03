using System.Text.Json.Nodes;
using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Presentation.Look;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>
/// Categorizing a transaction from the list: the "Zaradiť" box, the box about fingerprints, the mark and undoing it
/// (work/022-categorize-from-list/spec.md). The window's part is in DocuSpend.Desktop.Tests.
/// </summary>
public sealed class CategorizeTests : IDisposable
{
    private readonly string _folder = Path.Join(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    private string RulesFile => Path.Join(_folder, "rules.json");

    private string SettingsFile => Path.Join(_folder, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private static readonly SubcategoryId Taxi = new("transport.taxi");
    private static readonly SubcategoryId Games = new("leisure.games");

    private async Task<MainViewModel> Start(string sample = "01-typical-month")
    {
        var main = new MainViewModel(new RuleStore(RulesFile), new SettingsStore(SettingsFile));
        await main.OpenFileAsync(Samples.Path(sample));
        Assert.True(main.HasStatement);
        return main;
    }

    /// <summary>Shows the category the transaction is in and returns its row, as a user finds it.</summary>
    private static TransactionRow Row(MainViewModel main, string description, int nth = 0)
    {
        var all = main.Statement.Categorized!.Transactions;
        var index = Enumerable.Range(0, all.Count).Where(i => all[i].Transaction.Description == description).ElementAt(nth);
        main.Statement.SelectedCategory = all[index].Placement.Category;
        return main.Statement.Transactions.Single(r => r.Index == index);
    }

    private static string Where(MainViewModel main, string description, int nth = 0)
    {
        var placement = main.Statement.Categorized!.Transactions.Where(t => t.Transaction.Description == description).ElementAt(nth).Placement;
        return placement.Subcategory?.Value ?? placement.Category.Value;
    }

    private static void Target(RuleFormViewModel form, SubcategoryId target)
    {
        form.SelectedCategory = form.Categories.Single(c => c.Subcategories.Any(s => s.Id == target));
        form.SelectedSubcategory = form.Subcategories.Single(s => s.Id == target);
    }

    /// <summary>Opens the box on the row, chooses the target, and confirms "Len túto transakciu", the box about fingerprints included.</summary>
    private static void JustThis(MainViewModel main, TransactionRow row, SubcategoryId target)
    {
        main.CategorizeCommand.Execute(row);
        Target(main.Categorizing!.Form, target);
        main.ConfirmCategorizeCommand.Execute(null);
        if (main.Categorizing is { ShowsPrivacy: true })
            main.ConfirmPrivacyCommand.Execute(null);
        Assert.Null(main.Categorizing);
    }

    private static Statement StatementOf(params Transaction[] transactions)
    {
        var sum = transactions.Sum(t => t.Amount);
        return new Statement
        {
            AccountHolder = "TEST", Iban = "SK00", Bic = "TESTSKXX", Currency = "EUR",
            PeriodStart = new DateOnly(2026, 9, 1), PeriodEnd = new DateOnly(2026, 9, 30),
            OpeningBalance = 1000m, ClosingBalance = 1000m + sum,
            TotalDeposits = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
            TotalWithdrawals = transactions.Where(t => t.Amount < 0).Sum(t => t.Amount),
            TotalFees = 0m,
            Transactions = transactions,
        };
    }

    private static Transaction Stall(decimal amount = -2.40m) => new()
    {
        BookingDate = new DateOnly(2026, 9, 12), ValueDate = new DateOnly(2026, 9, 12), Type = TransactionType.CardPayment,
        Amount = amount, Currency = "EUR", Description = "STANOK U JOZEFA",
    };

    // Choosing a transaction and the box

    [Fact]
    public async Task A_selected_row_is_cleared_when_another_category_is_chosen()
    {
        var main = await Start();
        main.Statement.SelectedTransaction = Row(main, "NETFLIX.COM");

        main.Statement.SelectedCategory = main.Statement.Categories.First(c => c.Category != main.Statement.SelectedCategory).Category;

        Assert.Null(main.Statement.SelectedTransaction);
    }

    [Fact]
    public async Task Zaradit_opens_the_box_filled_in_from_the_transaction_and_Zrusit_changes_nothing()
    {
        var main = await Start();
        var row = Row(main, "NETFLIX.COM");
        var before = File.Exists(RulesFile);

        main.CategorizeCommand.Execute(row);

        var box = main.Categorizing!;
        Assert.Equal(("7. 3. 2026", "NETFLIX.COM", row.Amount), (box.Date, box.Row.Description, box.Row.Amount));
        Assert.True(box.JustThis);
        Assert.Equal((ConditionKind.Text, "NETFLIX.COM"), (box.Form.SelectedKind, box.Form.Text));
        Assert.Equal(row.Target, box.Form.SelectedSubcategory?.Id);
        Assert.DoesNotContain(box.Form.Categories, c => c.IsUncategorized);

        main.CancelCategorizeCommand.Execute(null);
        Assert.Null(main.Categorizing);
        Assert.Equal(before, File.Exists(RulesFile));
        Assert.Equal(row.Target?.Value, Where(main, "NETFLIX.COM"));
    }

    [Fact]
    public async Task An_uncategorized_transaction_starts_with_no_sub_category_and_asks_for_one()
    {
        var main = await Start();
        main.Statement.SelectedCategory = new CategoryId("uncategorized-out");

        main.CategorizeCommand.Execute(main.Statement.Transactions[0]);
        Assert.Null(main.Categorizing!.Form.SelectedSubcategory);
        main.ConfirmCategorizeCommand.Execute(null);

        Assert.Equal("Vyberte podkategóriu.", main.Categorizing.Form.Error);
        Assert.False(main.Categorizing.ShowsPrivacy);
    }

    // Just this transaction

    [Fact]
    public async Task Just_this_transaction_moves_it_alone_says_so_and_is_saved_before_the_message()
    {
        var main = await Start();
        var spending = main.Statement.SpendingTotal;

        JustThis(main, Row(main, "MINIMARKET U LIPY TRENCIN", 1), new SubcategoryId("own-transfers.other"));

        Assert.Equal(["food.groceries", "own-transfers.other", "food.groceries"],
            Enumerable.Range(0, 3).Select(i => Where(main, "MINIMARKET U LIPY TRENCIN", i)));
        Assert.Equal("Transakcia je zaradená do „Prevody medzi vlastnými účtami › Ostatné“.", main.Statement.TransactionsMessage);
        Assert.Null(main.Statement.TransactionsError);
        Assert.NotEqual(spending, main.Statement.SpendingTotal);
        Assert.Single(JsonNode.Parse(File.ReadAllText(RulesFile))!["handChoices"]!.AsArray());
    }

    [Fact]
    public async Task Just_this_transaction_leaves_its_identical_twin_where_it_was()
    {
        var main = await Start();
        main.Statement.Show(StatementOf(Stall(), Stall(), Stall(-5m)));
        main.Statement.SelectedCategory = new CategoryId("uncategorized-out");

        JustThis(main, main.Statement.Transactions[1], Taxi);

        var placed = main.Statement.Categorized!.Transactions.Select(t => t.Placement.Subcategory?.Value ?? t.Placement.Category.Value);
        Assert.Equal(["uncategorized-out", "transport.taxi", "uncategorized-out"], placed);
    }

    [Fact]
    public async Task The_message_stays_when_the_category_it_was_in_is_left_empty()
    {
        var main = await Start();
        main.Statement.Show(StatementOf(Stall()));
        main.Statement.SelectedCategory = new CategoryId("uncategorized-out");

        JustThis(main, main.Statement.Transactions[0], Taxi);

        Assert.Null(main.Statement.SelectedCategory);
        Assert.Equal("Transakcia je zaradená do „Doprava › Taxi“.", main.Statement.TransactionsMessage);
    }

    [Fact]
    public async Task A_hand_choice_beats_the_rules_also_one_added_later_and_a_second_one_replaces_it()
    {
        var main = await Start();
        JustThis(main, Row(main, "NETFLIX.COM"), Taxi);

        main.RuleForm.Text = "netflix";
        Target(main.RuleForm, Games);
        main.AddRuleCommand.Execute(null);
        Assert.Equal("transport.taxi", Where(main, "NETFLIX.COM"));

        JustThis(main, Row(main, "NETFLIX.COM"), new SubcategoryId("health.doctors"));
        Assert.Equal("health.doctors", Where(main, "NETFLIX.COM"));
        Assert.Single(main.Statement.Config.HandChoices);
    }

    [Fact]
    public async Task A_hand_choice_is_back_after_a_restart_with_its_mark()
    {
        var main = await Start();
        JustThis(main, Row(main, "NETFLIX.COM"), Taxi);

        var restarted = await Start();

        Assert.Equal("transport.taxi", Where(restarted, "NETFLIX.COM"));
        Assert.True(Row(restarted, "NETFLIX.COM").ByHand);
        Assert.False(Row(restarted, "PEKAREN KLAS TRENCIN").ByHand);
    }

    // A rule from a transaction

    [Fact]
    public async Task A_rule_from_a_transaction_is_an_ordinary_user_rule_on_top()
    {
        var main = await Start();
        main.CategorizeCommand.Execute(Row(main, "MINIMARKET U LIPY TRENCIN"));
        var box = main.Categorizing!;
        box.ByRule = true;
        Target(box.Form, Games);

        main.ConfirmCategorizeCommand.Execute(null);

        Assert.Null(main.Categorizing);
        Assert.Equal(new TextCondition("MINIMARKET U LIPY TRENCIN", TextMode.Contains), main.Statement.Config.UserRules.Single().Condition);
        Assert.Equal(["leisure.games", "leisure.games", "leisure.games"], Enumerable.Range(0, 3).Select(i => Where(main, "MINIMARKET U LIPY TRENCIN", i)));
        Assert.Equal("Pravidlo zaradilo 3 transakcie.", main.Statement.TransactionsMessage);
        Assert.Equal("Text obsahuje „MINIMARKET U LIPY TRENCIN“", main.Rules[0].Condition);
        Assert.Empty(main.Statement.Config.HandChoices);
    }

    [Fact]
    public async Task A_condition_that_misses_the_transaction_is_refused_and_the_box_stays()
    {
        var main = await Start();
        main.CategorizeCommand.Execute(Row(main, "NETFLIX.COM"));
        var box = main.Categorizing!;
        box.ByRule = true;
        box.Form.Text = "SPOTIFY";
        Target(box.Form, Games);

        main.ConfirmCategorizeCommand.Execute(null);

        Assert.Same(box, main.Categorizing);
        Assert.Equal("Táto transakcia podmienke nezodpovedá. Upravte podmienku.", box.Form.Error);
        Assert.Empty(main.Statement.Config.UserRules);
    }

    [Theory]
    [InlineData("text", "Zadajte text, ktorý obsahuje aspoň jedno písmeno alebo číslicu.")]
    [InlineData("amount", "Zadajte sumu od, sumu do alebo obe; nesmú byť záporné a suma od nesmie byť vyššia ako suma do.")]
    [InlineData("type", "Vyberte typ transakcie.")]
    public async Task An_invalid_condition_gives_the_reason_of_the_rules_screen(string what, string reason)
    {
        var main = await Start();
        main.CategorizeCommand.Execute(Row(main, "NETFLIX.COM"));
        var form = main.Categorizing!.Form;
        main.Categorizing.ByRule = true;
        Target(form, Games);
        switch (what)
        {
            case "text": form.Text = "  "; break;
            case "amount": form.SelectedKind = ConditionKind.Amount; (form.AmountFrom, form.AmountTo) = ("20", "10"); break;
            default: form.SelectedKind = ConditionKind.Type; form.Types.Single(t => t.IsChecked).IsChecked = false; break;
        }

        main.ConfirmCategorizeCommand.Execute(null);

        Assert.Equal(reason, form.Error);
        Assert.Empty(main.Statement.Config.UserRules);
    }

    [Fact]
    public async Task A_rule_made_from_a_hand_chosen_transaction_takes_over_from_its_hand_choice_only()
    {
        var main = await Start();
        JustThis(main, Row(main, "PEKAREN KLAS TRENCIN", 0), Taxi);
        JustThis(main, Row(main, "PEKAREN KLAS TRENCIN", 1), new SubcategoryId("health.doctors"));

        main.CategorizeCommand.Execute(Row(main, "PEKAREN KLAS TRENCIN", 0));
        main.Categorizing!.ByRule = true;
        Target(main.Categorizing.Form, Games);
        main.ConfirmCategorizeCommand.Execute(null);

        Assert.Equal(("leisure.games", "health.doctors"), (Where(main, "PEKAREN KLAS TRENCIN", 0), Where(main, "PEKAREN KLAS TRENCIN", 1)));
        Assert.Single(main.Statement.Config.HandChoices);
        Assert.Equal("Pravidlo zaradilo 1 transakciu.", main.Statement.TransactionsMessage);
    }

    // The box about fingerprints

    [Fact]
    public async Task The_box_about_fingerprints_opens_before_a_hand_choice_with_its_tick_off()
    {
        var main = await Start();
        main.CategorizeCommand.Execute(Row(main, "NETFLIX.COM"));
        Target(main.Categorizing!.Form, Taxi);

        main.ConfirmCategorizeCommand.Execute(null);

        Assert.True(main.Categorizing.ShowsPrivacy);
        Assert.False(main.Categorizing.HideNoteNextTime);
        Assert.Empty(main.Statement.Config.HandChoices);
    }

    [Fact]
    public void The_box_about_fingerprints_says_exactly_what_the_spec_says()
    {
        Assert.Equal("Čo si DocuSpend zapamätá", Texts.FingerprintHeading);
        Assert.Equal("Aby DocuSpend túto transakciu zaradil rovnako aj nabudúce, uloží si jej odtlačok. Neuloží jej dátum, sumu, popis ani protistranu, len odtlačok vypočítaný z nich. Odtlačok zostáva len v tomto počítači, v priečinku DocuSpend, a nikam sa neposiela.", Texts.FingerprintWhat);
        Assert.Equal("Z odtlačku sa tieto údaje nedajú spätne zistiť. Kto by však poznal presné údaje transakcie, napríklad z toho istého výpisu, a mal by váš súbor rules.json, vedel by overiť, či ste túto transakciu zaradili ručne.", Texts.FingerprintLimits);
        Assert.Equal("Odtlačok je uložený spolu s vašimi pravidlami v súbore %APPDATA%\\DocuSpend\\rules.json. Vymaže sa, keď zaradenie zrušíte tlačidlom „Zrušiť zaradenie“. Všetko, čo si DocuSpend pamätá, odstránite vymazaním priečinka %APPDATA%\\DocuSpend.", Texts.FingerprintWhere);
        Assert.Equal(("Túto správu už nezobrazovať", "Späť"), (Texts.DontShowAgain, Texts.Back));
    }

    [Fact]
    public async Task Spat_and_Escape_go_back_to_the_box_as_it_was_and_drop_the_tick()
    {
        var main = await Start();
        main.CategorizeCommand.Execute(Row(main, "NETFLIX.COM"));
        var box = main.Categorizing!;
        Target(box.Form, Taxi);

        main.ConfirmCategorizeCommand.Execute(null);
        box.HideNoteNextTime = true;
        main.BackFromPrivacyCommand.Execute(null);
        Assert.Equal((false, false, Taxi), (box.ShowsPrivacy, box.HideNoteNextTime, box.Form.SelectedSubcategory!.Id));

        main.ConfirmCategorizeCommand.Execute(null);
        main.EscapeCommand.Execute(null);
        Assert.Same(box, main.Categorizing);
        Assert.False(box.ShowsPrivacy);

        main.EscapeCommand.Execute(null);
        Assert.Null(main.Categorizing);
        Assert.Empty(main.Statement.Config.HandChoices);
    }

    [Fact]
    public async Task Without_the_tick_the_box_shows_again_and_with_it_never_again_also_after_a_restart()
    {
        var main = await Start();
        JustThis(main, Row(main, "NETFLIX.COM"), Taxi);

        main.CategorizeCommand.Execute(Row(main, "PEKAREN KLAS TRENCIN"));
        Target(main.Categorizing!.Form, Taxi);
        main.ConfirmCategorizeCommand.Execute(null);
        Assert.True(main.Categorizing.ShowsPrivacy);
        main.Categorizing.HideNoteNextTime = true;
        main.ConfirmPrivacyCommand.Execute(null);

        main.CategorizeCommand.Execute(Row(main, "BISTRO ZELENA LUKA TRENCIN"));
        Target(main.Categorizing!.Form, Taxi);
        main.ConfirmCategorizeCommand.Execute(null);
        Assert.Null(main.Categorizing);

        var restarted = await Start();
        restarted.CategorizeCommand.Execute(Row(restarted, "KNIHY A PAPIER TRENCIN"));
        Target(restarted.Categorizing!.Form, Taxi);
        restarted.ConfirmCategorizeCommand.Execute(null);
        Assert.Null(restarted.Categorizing);
        Assert.Equal("transport.taxi", Where(restarted, "KNIHY A PAPIER TRENCIN"));
    }

    [Fact]
    public async Task A_rule_needs_no_box_about_fingerprints()
    {
        var main = await Start();
        main.CategorizeCommand.Execute(Row(main, "NETFLIX.COM"));
        main.Categorizing!.ByRule = true;
        Target(main.Categorizing.Form, Taxi);

        main.ConfirmCategorizeCommand.Execute(null);

        Assert.Null(main.Categorizing);
        Assert.Single(main.Statement.Config.UserRules);
    }

    // The mark and undoing it

    [Fact]
    public async Task Undoing_a_hand_choice_gives_the_transaction_back_to_the_rules_and_lasts()
    {
        var main = await Start();
        JustThis(main, Row(main, "NETFLIX.COM"), Taxi);
        var row = Row(main, "NETFLIX.COM");
        Assert.True(row.ByHand);

        main.UndoHandChoiceCommand.Execute(row);

        Assert.Equal("Zaradenie je zrušené. Transakcia je opäť v „Nákupy online › Webové služby“.", main.Statement.TransactionsMessage);
        Assert.Equal("online-shopping.web-services", Where(main, "NETFLIX.COM"));
        Assert.False(Row(main, "NETFLIX.COM").ByHand);
        Assert.Empty(JsonNode.Parse(File.ReadAllText(RulesFile))!["handChoices"]!.AsArray());

        var restarted = await Start();
        Assert.False(Row(restarted, "NETFLIX.COM").ByHand);
    }

    [Fact]
    public async Task Undoing_names_the_Uncategorized_bucket_the_transaction_goes_back_to()
    {
        var main = await Start();
        main.Statement.Show(StatementOf(Stall()));
        main.Statement.SelectedCategory = new CategoryId("uncategorized-out");
        JustThis(main, main.Statement.Transactions[0], Taxi);
        main.Statement.SelectedCategory = new CategoryId("transport");

        main.UndoHandChoiceCommand.Execute(main.Statement.Transactions[0]);

        Assert.Equal("Zaradenie je zrušené. Transakcia je opäť v „Nezaradené výdavky“.", main.Statement.TransactionsMessage);
    }

    [Fact]
    public async Task A_transaction_a_rule_placed_has_no_mark_and_nothing_to_undo()
    {
        var main = await Start();
        var row = Row(main, "NETFLIX.COM");

        main.UndoHandChoiceCommand.Execute(row);

        Assert.False(row.ByHand);
        Assert.Null(main.Statement.TransactionsMessage);
        Assert.False(File.Exists(RulesFile));
    }

    // When a change can't be saved

    [Fact]
    public async Task While_saving_is_off_a_hand_choice_and_its_undo_say_they_last_until_closing()
    {
        Directory.CreateDirectory(_folder);
        const string Newer = """{ "version": 99 }""";
        File.WriteAllText(RulesFile, Newer);
        var main = await Start();

        JustThis(main, Row(main, "NETFLIX.COM"), Taxi);
        Assert.Equal("transport.taxi", Where(main, "NETFLIX.COM"));
        Assert.Equal("Zaradenie platí len do zatvorenia DocuSpend.", main.Statement.TransactionsError);

        main.UndoHandChoiceCommand.Execute(Row(main, "NETFLIX.COM"));
        Assert.Equal("Zaradenie sa po zatvorení DocuSpend vráti.", main.Statement.TransactionsError);
        Assert.Equal(Newer, File.ReadAllText(RulesFile));
    }

    [Fact]
    public async Task When_saving_fails_a_hand_choice_and_its_undo_say_so()
    {
        var main = await Start();
        Directory.CreateDirectory(RulesFile);

        JustThis(main, Row(main, "NETFLIX.COM"), Taxi);
        Assert.Equal(Texts.HandChoiceSaveFailed, main.Statement.TransactionsError);
        Assert.Equal("transport.taxi", Where(main, "NETFLIX.COM"));

        main.UndoHandChoiceCommand.Execute(Row(main, "NETFLIX.COM"));
        Assert.Equal(Texts.UndoSaveFailed, main.Statement.TransactionsError);
    }

    [Fact]
    public async Task A_rule_from_a_transaction_that_cannot_be_saved_says_so_as_on_the_rules_screen()
    {
        var main = await Start();
        Directory.CreateDirectory(RulesFile);
        main.CategorizeCommand.Execute(Row(main, "NETFLIX.COM"));
        main.Categorizing!.ByRule = true;
        Target(main.Categorizing.Form, Taxi);

        main.ConfirmCategorizeCommand.Execute(null);

        Assert.Equal(Texts.RuleSaveFailed, main.Statement.TransactionsError);
        Assert.Single(main.Statement.Config.UserRules);
    }
}
