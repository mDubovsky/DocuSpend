using DocuSpend.Statements.Reading;

namespace DocuSpend.Presentation.Tests;

/// <summary>The messages for a file that wasn't opened (spec: when a file can't be opened).</summary>
public class OpenMessagesTests
{
    private const string SlovakLetters = "áäčďéíĺľňóôŕšťúýžÁÄČĎÉÍĹĽŇÓÔŔŠŤÚÝŽ";

    public static TheoryData<OpenProblem> Problems => new(Enum.GetValues<OpenProblem>());
    public static TheoryData<RejectionKind> Kinds => new(Enum.GetValues<RejectionKind>());

    public static TheoryData<string> Messages => new(
        Enum.GetValues<OpenProblem>().Select(Texts.Problem)
            .Append(Texts.NotOpened).Append(Texts.PreviousStatementKept)
            .Append(Texts.DropSeveralFiles).Append(Texts.DropNotAFile));

    [Theory]
    [MemberData(nameof(Problems))]
    public void Every_problem_has_a_message(OpenProblem problem)
    {
        var message = Texts.Problem(problem);

        Assert.False(string.IsNullOrWhiteSpace(message));
        Assert.EndsWith(".", message);
    }

    [Fact]
    public void Every_problem_has_its_own_message()
    {
        var messages = Enum.GetValues<OpenProblem>().Select(Texts.Problem).ToList();

        Assert.Equal(messages.Count, messages.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Messages))]
    public void Every_message_is_slovak(string message)
    {
        // A cheap guard against an English message slipping in: every one of them has Slovak letters.
        Assert.Contains(message, c => SlovakLetters.Contains(c));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Every_kind_of_rejection_is_a_problem_with_a_message(RejectionKind kind) =>
        Assert.False(string.IsNullOrWhiteSpace(Texts.Problem(OpenProblems.From(kind))));

    [Fact]
    public void Each_kind_of_rejection_is_its_own_problem()
    {
        var problems = Enum.GetValues<RejectionKind>().Select(OpenProblems.From).ToList();

        Assert.Equal(problems.Count, problems.Distinct().Count());
    }

    [Theory]
    [InlineData("06-other-bank-statement", OpenProblem.NotAVubStatement)]
    [InlineData("07-not-a-statement", OpenProblem.NotAVubStatement)]
    [InlineData("08-corrupted-statement", OpenProblem.NotAPdf)]
    [InlineData("09-empty-file", OpenProblem.Empty)]
    public void Invalid_samples_are_told_apart_and_their_messages_quote_nothing_from_them(string name, OpenProblem expected)
    {
        var rejection = new VubStatementReader().Read(File.ReadAllBytes(Path.Combine(Samples.Dir, name + ".pdf"))).Rejection!;
        var problem = OpenProblems.From(rejection.Kind);

        Assert.Equal(expected, problem);
        foreach (var kept in new[] { false, true })
        {
            var message = Texts.NotOpenedMessage(problem, kept);
            Assert.DoesNotContain(rejection.Message, message);
            Assert.DoesNotContain(name, message);
            Assert.DoesNotContain(message, char.IsAsciiDigit);
        }
    }

    [Fact]
    public void The_message_says_when_the_previous_statement_stays()
    {
        Assert.Equal(
            "Výpis sa neotvoril. Súbor je príliš veľký na mesačný výpis.",
            Texts.NotOpenedMessage(OpenProblem.TooLarge, previousStatementKept: false));
        Assert.Equal(
            "Výpis sa neotvoril. Súbor je príliš veľký na mesačný výpis. Zobrazený zostáva predchádzajúci výpis.",
            Texts.NotOpenedMessage(OpenProblem.TooLarge, previousStatementKept: true));
    }
}
