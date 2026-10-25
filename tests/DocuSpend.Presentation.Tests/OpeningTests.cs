using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.SampleGenerator.Rendering;
using DocuSpend.SampleGenerator.Scenarios;

namespace DocuSpend.Presentation.Tests;

/// <summary>Opening a statement the user chose (spec: starting; opening a statement; when a file can't be opened).</summary>
public sealed class OpeningTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("docuspend-opening-tests-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private MainViewModel Start(string? example = null) =>
        new(new SpikeRuleStore(Path.Combine(_folder, "rules.json")), example: example);

    private async Task<MainViewModel> StartWith(string sample)
    {
        var main = Start();
        await main.OpenFileAsync(Samples.Path(sample));
        Assert.True(main.HasStatement);
        return main;
    }

    private string Write(string name, byte[] bytes)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    /// <summary>A file the picker hands over, or null for a cancelled open window; it counts how often it was asked.</summary>
    private sealed class TestPicker(string? path) : IStatementPicker
    {
        public int Asked { get; private set; }

        public Task<string?> PickAsync()
        {
            Asked++;
            return Task.FromResult(path);
        }
    }

    public static TheoryData<string> Valid => new(Samples.Valid);

    public static TheoryData<string, OpenProblem> Invalid => new()
    {
        { "06-other-bank-statement", OpenProblem.NotAVubStatement },
        { "07-not-a-statement", OpenProblem.NotAVubStatement },
        { "08-corrupted-statement", OpenProblem.NotAPdf },
        { "09-empty-file", OpenProblem.Empty },
    };

    [Fact]
    public void DocuSpend_starts_on_the_start_screen()
    {
        var main = Start();

        Assert.False(main.HasStatement);
        Assert.False(main.IsExample);
        Assert.False(main.IsReading);
        Assert.Null(main.OpenMessage);
        Assert.Empty(main.Statement.Categories);
    }

    [Theory]
    [MemberData(nameof(Valid))]
    public async Task A_valid_statement_opened_from_disk_is_shown_as_the_reader_and_categorizer_give_it(string name)
    {
        var main = Start();

        await main.OpenFileAsync(Samples.Path(name));

        var statement = Samples.Read(name);
        var expected = Categorizer.Categorize(statement, CategorizationConfig.Default);
        Assert.True(main.HasStatement);
        Assert.False(main.IsExample);
        Assert.Null(main.OpenMessage);
        Assert.Equal(SlovakFormat.Period(statement.PeriodStart, statement.PeriodEnd), main.Statement.Period);
        Assert.Equal(SlovakFormat.Amount(statement.ClosingBalance), main.Statement.ClosingBalance);
        Assert.Equal(expected.Transactions.Select(t => t.Placement), main.Statement.Categorized!.Transactions.Select(t => t.Placement));
    }

    [Theory]
    [MemberData(nameof(Invalid))]
    public async Task An_invalid_sample_on_the_start_screen_gives_its_message_and_stays_on_the_start_screen(string name, OpenProblem problem)
    {
        var main = Start();

        await main.OpenFileAsync(Samples.Path(name));

        Assert.False(main.HasStatement);
        Assert.Equal(Texts.NotOpenedMessage(problem, previousStatementKept: false), main.OpenMessage);
        Assert.Empty(main.Statement.Categories);
    }

    [Theory]
    [MemberData(nameof(Invalid))]
    public async Task An_invalid_sample_over_a_statement_gives_its_message_and_keeps_the_statement(string name, OpenProblem problem)
    {
        var main = await StartWith("02-busy-month");
        main.Statement.SelectedCategory = new CategoryId("uncategorized-out");
        var before = main.Statement.Categorized;

        await main.OpenFileAsync(Samples.Path(name));

        Assert.True(main.HasStatement);
        Assert.Equal(Texts.NotOpenedMessage(problem, previousStatementKept: true), main.OpenMessage);
        Assert.Same(before, main.Statement.Categorized);
        Assert.Equal(new CategoryId("uncategorized-out"), main.Statement.SelectedCategory);
        Assert.NotEmpty(main.Statement.Transactions);
    }

    [Fact]
    public async Task A_statement_that_does_not_add_up_gives_its_own_message()
    {
        var scenario = ValidSamples.SingleTransactionMonth();
        var path = Write("does-not-add-up.pdf", VubStatementRenderer.Render(scenario,
            new RenderOptions { PrintedTotals = scenario.Totals with { ClosingBalance = scenario.Totals.ClosingBalance + 1m } }));
        var main = Start();

        await main.OpenFileAsync(path);

        Assert.False(main.HasStatement);
        Assert.Equal(Texts.NotOpenedMessage(OpenProblem.DoesNotReconcile, false), main.OpenMessage);
    }

    [Fact]
    public async Task A_statement_with_a_part_that_cannot_be_read_gives_its_own_message()
    {
        string[] summary = ["Vlastné použiteľné prostriedky", "Vklady spolu", "Výbery spolu", "Poplatky spolu"];
        var path = Write("unreadable.pdf", VubStatementRenderer.Render(ValidSamples.SingleTransactionMonth(), new RenderOptions
        {
            TextTransform = t => summary.Contains(t) || t.StartsWith("Vlast. použit.", StringComparison.Ordinal) ? null : t,
        }));
        var main = Start();

        await main.OpenFileAsync(path);

        Assert.Equal(Texts.NotOpenedMessage(OpenProblem.Unreadable, false), main.OpenMessage);
    }

    [Fact]
    public async Task A_deleted_file_cannot_be_opened()
    {
        var main = await StartWith("01-typical-month");

        await main.OpenFileAsync(Path.Combine(_folder, "deleted.pdf"));

        Assert.Equal(Texts.NotOpenedMessage(OpenProblem.CannotOpen, true), main.OpenMessage);
    }

    [Fact]
    public async Task A_locked_file_cannot_be_opened()
    {
        var path = Write("locked.pdf", File.ReadAllBytes(Samples.Path("01-typical-month")));
        using var holder = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var main = Start();

        await main.OpenFileAsync(path);

        Assert.Equal(Texts.NotOpenedMessage(OpenProblem.CannotOpen, false), main.OpenMessage);
    }

    [Fact]
    public async Task A_file_over_the_limit_is_too_large()
    {
        var path = Path.Combine(_folder, "huge.pdf");
        using (var file = new FileStream(path, FileMode.CreateNew))
            file.SetLength(StatementFile.MaxBytes + 1);
        var main = Start();

        await main.OpenFileAsync(path);

        Assert.Equal(Texts.NotOpenedMessage(OpenProblem.TooLarge, false), main.OpenMessage);
    }

    [Fact]
    public async Task A_statement_saved_without_its_extension_opens()
    {
        var path = Write("vypis", File.ReadAllBytes(Samples.Path("01-typical-month")));
        var main = Start();

        await main.OpenFileAsync(path);

        Assert.True(main.HasStatement);
    }

    [Fact]
    public async Task Another_statement_replaces_the_shown_one_with_nothing_selected_and_the_form_kept()
    {
        var main = await StartWith("01-typical-month");
        main.Statement.SelectedCategory = new CategoryId("uncategorized-out");
        main.RuleForm.Text = "rozpísané";

        await main.OpenFileAsync(Samples.Path("02-busy-month"));

        var busy = Samples.Read("02-busy-month");
        Assert.Equal(SlovakFormat.Period(busy.PeriodStart, busy.PeriodEnd), main.Statement.Period);
        Assert.Null(main.Statement.SelectedCategory);
        Assert.Equal("rozpísané", main.RuleForm.Text);
    }

    [Fact]
    public async Task The_same_statement_opened_again_is_shown_again_with_nothing_selected()
    {
        var main = await StartWith("01-typical-month");
        main.Statement.SelectedCategory = new CategoryId("uncategorized-out");

        await main.OpenFileAsync(Samples.Path("01-typical-month"));

        Assert.Null(main.Statement.SelectedCategory);
        Assert.Null(main.OpenMessage);
    }

    [Fact]
    public async Task A_statement_that_opens_removes_the_message()
    {
        var main = await StartWith("01-typical-month");
        await main.OpenFileAsync(Samples.Path("07-not-a-statement"));
        Assert.NotNull(main.OpenMessage);

        await main.OpenFileAsync(Samples.Path("02-busy-month"));

        Assert.Null(main.OpenMessage);
    }

    [Fact]
    public async Task Closing_the_message_keeps_the_statement()
    {
        var main = await StartWith("01-typical-month");
        await main.OpenFileAsync(Samples.Path("07-not-a-statement"));

        main.CloseMessageCommand.Execute(null);

        Assert.Null(main.OpenMessage);
        Assert.True(main.HasStatement);
        Assert.NotEmpty(main.Statement.Categories);
    }

    [Fact]
    public async Task The_example_is_offered_opened_and_labelled()
    {
        var main = Start(example: Samples.Path("01-typical-month"));
        Assert.True(main.HasExample);
        Assert.True(main.OpenExampleCommand.CanExecute(null));

        await main.OpenExampleCommand.ExecuteAsync(null);

        Assert.True(main.HasStatement);
        Assert.True(main.IsExample);
        var example = Samples.Read("01-typical-month");
        Assert.Equal(SlovakFormat.Period(example.PeriodStart, example.PeriodEnd), main.Statement.Period);
    }

    [Fact]
    public async Task A_statement_opened_after_the_example_is_not_labelled()
    {
        var main = Start(example: Samples.Path("01-typical-month"));
        await main.OpenExampleCommand.ExecuteAsync(null);

        await main.OpenFileAsync(Samples.Path("02-busy-month"));

        Assert.False(main.IsExample);
    }

    [Fact]
    public async Task A_file_that_fails_after_the_example_keeps_the_label()
    {
        var main = Start(example: Samples.Path("01-typical-month"));
        await main.OpenExampleCommand.ExecuteAsync(null);

        await main.OpenFileAsync(Samples.Path("09-empty-file"));

        Assert.True(main.IsExample);
    }

    [Fact]
    public void A_missing_example_is_not_offered()
    {
        var main = Start(example: Path.Combine(_folder, "missing.pdf"));

        Assert.False(main.HasExample);
        Assert.False(main.OpenExampleCommand.CanExecute(null));
    }

    [Fact]
    public void Without_an_example_none_is_offered() => Assert.False(Start().HasExample);

    [Fact]
    public async Task The_open_command_opens_what_the_picker_chose()
    {
        var main = Start();
        var picker = new TestPicker(Samples.Path("03-awkward-month"));
        main.Picker = picker;

        await main.OpenCommand.ExecuteAsync(null);

        Assert.Equal(1, picker.Asked);
        Assert.True(main.HasStatement);
    }

    [Fact]
    public async Task Cancelling_the_open_window_changes_nothing()
    {
        var main = await StartWith("01-typical-month");
        await main.OpenFileAsync(Samples.Path("07-not-a-statement"));
        var message = main.OpenMessage;
        var before = main.Statement.Categorized;
        main.Picker = new TestPicker(null);

        await main.OpenCommand.ExecuteAsync(null);

        Assert.Same(before, main.Statement.Categorized);
        Assert.Equal(message, main.OpenMessage);
    }

    [Fact]
    public async Task Cancelling_on_the_start_screen_shows_nothing()
    {
        var main = Start();
        main.Picker = new TestPicker(null);

        await main.OpenCommand.ExecuteAsync(null);

        Assert.False(main.HasStatement);
        Assert.Null(main.OpenMessage);
    }

    [Fact]
    public async Task A_file_opened_while_another_is_read_is_ignored()
    {
        var main = Start();

        // Reading starts at once, before the first await, so the second call finds it busy.
        var first = main.OpenFileAsync(Samples.Path("03-awkward-month"));
        Assert.True(main.IsReading);
        await main.OpenFileAsync(Samples.Path("04-empty-month"));
        await main.DropAsync(new DroppedContent([Samples.Path("04-empty-month")], 0));
        await first;

        var awkward = Samples.Read("03-awkward-month");
        Assert.Equal(SlovakFormat.Period(awkward.PeriodStart, awkward.PeriodEnd), main.Statement.Period);
        Assert.False(main.IsReading);
    }

    [Fact]
    public async Task Reading_marks_the_view_model_as_busy_and_disables_opening()
    {
        var main = Start(example: Samples.Path("01-typical-month"));
        var seen = new List<(bool Reading, bool CanOpen, bool CanOpenExample)>();
        main.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsReading))
                seen.Add((main.IsReading, main.OpenCommand.CanExecute(null), main.OpenExampleCommand.CanExecute(null)));
        };

        await main.OpenFileAsync(Samples.Path("01-typical-month"));

        Assert.Equal([(true, false, false), (false, true, true)], seen);
    }

    [Fact]
    public async Task A_dropped_file_opens_like_a_chosen_one()
    {
        var main = Start();
        main.IsDraggingOver = true;

        await main.DropAsync(new DroppedContent([Samples.Path("05-single-transaction-month")], 0));

        Assert.True(main.HasStatement);
        Assert.False(main.IsDraggingOver);
    }

    [Fact]
    public async Task A_dropped_invalid_file_gives_its_message()
    {
        var main = await StartWith("01-typical-month");

        await main.DropAsync(new DroppedContent([Samples.Path("06-other-bank-statement")], 0));

        Assert.Equal(Texts.NotOpenedMessage(OpenProblem.NotAVubStatement, true), main.OpenMessage);
    }

    [Theory]
    [InlineData(2, 0, Texts.DropSeveralFiles)]
    [InlineData(2, 1, Texts.DropSeveralFiles)]
    [InlineData(0, 1, Texts.DropNotAFile)]
    [InlineData(0, 2, Texts.DropNotAFile)]
    [InlineData(1, 1, Texts.DropNotAFile)]
    [InlineData(0, 0, Texts.DropNotAFile)]
    public async Task A_drop_that_is_not_one_file_gives_its_message_and_changes_nothing(int files, int others, string message)
    {
        var main = await StartWith("01-typical-month");
        var before = main.Statement.Categorized;
        main.IsDraggingOver = true;

        await main.DropAsync(new DroppedContent(Enumerable.Repeat(Samples.Path("02-busy-month"), files).ToList(), others));

        Assert.Equal(message, main.OpenMessage);
        Assert.Same(before, main.Statement.Categorized);
        Assert.False(main.IsDraggingOver);
    }

    [Fact]
    public async Task No_message_contains_anything_from_the_file_or_its_name()
    {
        var main = await StartWith("01-typical-month");
        foreach (var name in Samples.Invalid)
        {
            await main.OpenFileAsync(Samples.Path(name));
            Assert.DoesNotContain(name, main.OpenMessage);
            Assert.DoesNotContain(Samples.Dir, main.OpenMessage);
            Assert.DoesNotContain(main.OpenMessage!, char.IsAsciiDigit);
        }
    }
}
