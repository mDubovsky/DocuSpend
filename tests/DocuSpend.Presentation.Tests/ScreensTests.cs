using DocuSpend.Categorization.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>
/// The choice between the statement screen and the rules screen (work/021-rules-screen/spec.md). The window's part
/// is in DocuSpend.Desktop.Tests.
/// </summary>
public sealed class ScreensTests : IDisposable
{
    private readonly string _folder = Path.Join(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    private string RulesFile => Path.Join(_folder, "rules.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private async Task<MainViewModel> Start(string sample = "01-typical-month")
    {
        var main = new MainViewModel(new RuleStore(RulesFile));
        await main.OpenFileAsync(Samples.Path(sample));
        Assert.True(main.HasStatement);
        return main;
    }

    private static void GoToRules(MainViewModel main) => main.SelectedScreen = main.Screens.Single(s => s.Screen == AppScreen.Rules);

    private static void AddTextRule(MainViewModel main, string text, string categoryId)
    {
        var form = main.RuleForm;
        form.SelectedKind = ConditionKind.Text;
        form.Text = text;
        form.SelectedCategory = form.Categories.Single(c => c.Id == new CategoryId(categoryId));
        form.SelectedSubcategory = form.Subcategories[0];
        main.AddRuleCommand.Execute(null);
    }

    private sealed class TestPicker(string? path) : IStatementPicker
    {
        public Task<string?> PickAsync() => Task.FromResult(path);
    }

    [Fact]
    public void The_statement_screen_is_chosen_at_start_and_nothing_shows_without_a_statement()
    {
        var main = new MainViewModel(new RuleStore(RulesFile));

        Assert.Equal(AppScreen.Statement, main.SelectedScreen.Screen);
        Assert.False(main.ShowsStatement);
        Assert.False(main.ShowsRules);
    }

    [Fact]
    public void The_screens_are_offered_as_Vypis_then_Pravidla()
    {
        var main = new MainViewModel(new RuleStore(RulesFile));

        Assert.Equal(["Výpis", "Pravidlá"], main.Screens.Select(s => s.Label));
        Assert.Equal([AppScreen.Statement, AppScreen.Rules], main.Screens.Select(s => s.Screen));
    }

    [Fact]
    public async Task With_a_statement_open_either_screen_can_be_chosen_and_only_it_shows()
    {
        var main = await Start();

        Assert.True(main.ShowsStatement);
        Assert.False(main.ShowsRules);

        GoToRules(main);
        Assert.False(main.ShowsStatement);
        Assert.True(main.ShowsRules);

        main.SelectedScreen = main.Screens[0];
        Assert.True(main.ShowsStatement);
        Assert.False(main.ShowsRules);
    }

    [Fact]
    public async Task Choosing_the_chosen_screen_changes_nothing()
    {
        var main = await Start();
        var changes = new List<string?>();
        main.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

        main.SelectedScreen = main.Screens[0];

        Assert.DoesNotContain(nameof(MainViewModel.ShowsStatement), changes);
        Assert.DoesNotContain(nameof(MainViewModel.ShowsRules), changes);
        Assert.True(main.ShowsStatement);
    }

    [Fact]
    public async Task Opening_a_statement_with_the_button_shows_the_statement_screen_from_the_rules_screen()
    {
        var main = await Start();
        GoToRules(main);
        main.Picker = new TestPicker(Samples.Path("03-awkward-month"));

        await main.OpenCommand.ExecuteAsync(null);

        Assert.Equal(AppScreen.Statement, main.SelectedScreen.Screen);
        Assert.True(main.ShowsStatement);
    }

    [Fact]
    public async Task Opening_a_file_and_dropping_one_show_the_statement_screen_from_the_rules_screen()
    {
        var main = await Start();
        GoToRules(main);
        await main.OpenFileAsync(Samples.Path("02-busy-month"));
        Assert.True(main.ShowsStatement);

        GoToRules(main);
        await main.DropAsync(new DroppedContent([Samples.Path("05-single-transaction-month")], 0));
        Assert.True(main.ShowsStatement);
    }

    [Fact]
    public async Task Opening_the_example_from_the_start_screen_shows_the_statement_screen()
    {
        var main = new MainViewModel(new RuleStore(RulesFile), example: Samples.Path("01-typical-month"));

        await main.OpenExampleCommand.ExecuteAsync(null);

        Assert.True(main.ShowsStatement);
        Assert.Equal(AppScreen.Statement, main.SelectedScreen.Screen);
    }

    [Fact]
    public async Task A_file_that_fails_leaves_the_rules_screen_chosen_with_the_message_and_the_statement_unchanged()
    {
        var main = await Start();
        var period = main.Statement.Period;
        GoToRules(main);

        await main.OpenFileAsync(Samples.Path("06-other-bank-statement"));

        Assert.Equal(AppScreen.Rules, main.SelectedScreen.Screen);
        Assert.True(main.ShowsRules);
        Assert.NotNull(main.OpenMessage);
        Assert.Equal(period, main.Statement.Period);
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(0, 1)]
    public async Task A_wrong_drop_leaves_the_rules_screen_chosen_with_its_message(int files, int others)
    {
        var main = await Start();
        GoToRules(main);

        await main.DropAsync(new DroppedContent(Enumerable.Repeat(Samples.Path("02-busy-month"), files).ToList(), others));

        Assert.True(main.ShowsRules);
        Assert.NotNull(main.OpenMessage);
    }

    [Fact]
    public async Task A_rule_added_on_the_rules_screen_recategorizes_the_statement_and_says_how_many_it_catches()
    {
        var main = await Start();
        main.Statement.SelectedCategory = new CategoryId("uncategorized-out");
        var text = main.Statement.Transactions[0].Description;
        var before = main.Statement.Categories.FirstOrDefault(c => c.Category == new CategoryId("leisure"))?.Count ?? 0;
        GoToRules(main);

        AddTextRule(main, text, "leisure");

        Assert.NotNull(main.RuleForm.Message);
        Assert.Contains("zaradilo", main.RuleForm.Message);
        Assert.True(main.Statement.Categories.Single(c => c.Category == new CategoryId("leisure")).Count > before);
    }

    [Fact]
    public async Task The_selected_category_survives_a_round_trip_through_the_rules_screen()
    {
        var main = await Start();
        var category = main.Statement.Categories[1].Category;
        main.Statement.SelectedCategory = category;
        var transactions = main.Statement.Transactions.ToList();

        GoToRules(main);
        main.SelectedScreen = main.Screens[0];

        Assert.Equal(category, main.Statement.SelectedCategory);
        Assert.Equal(transactions, main.Statement.Transactions);
    }

    [Fact]
    public async Task A_category_a_rule_emptied_is_no_longer_selected_after_the_round_trip()
    {
        var main = await Start();
        main.Statement.SelectedCategory = new CategoryId("uncategorized-out");
        var statement = main.Statement;
        GoToRules(main);
        // One rule per description, until nothing is left in the selected category.
        foreach (var text in statement.Transactions.Select(t => t.Description).Distinct().ToList())
            AddTextRule(main, text, "leisure");
        main.SelectedScreen = main.Screens[0];

        Assert.Null(statement.SelectedCategory);
        Assert.Empty(statement.Transactions);
    }

    [Fact]
    public async Task A_half_typed_rule_survives_a_round_trip()
    {
        var main = await Start();
        GoToRules(main);
        main.RuleForm.SelectedKind = ConditionKind.CounterpartyAccount;
        main.RuleForm.Account = "SK12";

        main.SelectedScreen = main.Screens[0];
        GoToRules(main);

        Assert.Equal(ConditionKind.CounterpartyAccount, main.RuleForm.SelectedKind);
        Assert.Equal("SK12", main.RuleForm.Account);
    }

    [Fact]
    public async Task A_rule_being_edited_survives_a_round_trip_and_another_statement()
    {
        var main = await Start();
        GoToRules(main);
        AddTextRule(main, "lidl", "leisure");
        main.EditRuleCommand.Execute(main.Rules[0]);
        main.RuleForm.Text = "kaufland";

        main.SelectedScreen = main.Screens[0];
        await main.OpenFileAsync(Samples.Path("02-busy-month"));

        Assert.True(main.RuleForm.IsEditing);
        Assert.Equal("kaufland", main.RuleForm.Text);
        Assert.True(main.Rules[0].IsEditing);
    }

    [Fact]
    public async Task The_chosen_view_of_the_rules_card_survives_a_round_trip()
    {
        var main = await Start();
        GoToRules(main);
        main.SelectedRuleView = main.RuleViews[1];

        main.SelectedScreen = main.Screens[0];
        GoToRules(main);

        Assert.True(main.IsBuiltInView);
    }

    [Fact]
    public async Task While_a_file_is_read_the_chosen_screen_stays_and_the_statement_screen_comes_when_it_is_read()
    {
        var main = await Start();
        GoToRules(main);

        var reading = main.OpenFileAsync(Samples.Path("02-busy-month"));
        Assert.True(main.IsReading);
        Assert.True(main.ShowsRules);
        await reading;

        Assert.False(main.IsReading);
        Assert.True(main.ShowsStatement);
    }
}
