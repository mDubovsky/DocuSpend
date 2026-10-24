using DocuSpend.Categorization;

namespace DocuSpend.Presentation.Tests;

/// <summary>The window's state: which sample is shown (spec: starting and choosing a sample).</summary>
public class MainViewModelTests
{
    private static MainViewModel Window() => new(SampleCatalog.Available(Samples.Dir), CategorizationConfig.Default);

    [Fact]
    public void The_first_sample_is_shown_at_start()
    {
        var main = Window();

        Assert.Equal("01-typical-month", main.SelectedSample?.Name);
        Assert.Equal(SlovakFormat.Period(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)), main.Statement.Period);
        Assert.Null(main.Error);
    }

    [Theory]
    [MemberData(nameof(StatementViewModelTests.Valid), MemberType = typeof(StatementViewModelTests))]
    public void Choosing_a_sample_shows_it(string name)
    {
        var main = Window();
        var statement = Samples.Read(name);

        main.SelectedSample = main.Samples.Single(s => s.Name == name);

        Assert.Equal(SlovakFormat.Period(statement.PeriodStart, statement.PeriodEnd), main.Statement.Period);
        Assert.Equal(SlovakFormat.Amount(statement.ClosingBalance), main.Statement.ClosingBalance);
    }

    [Fact]
    public void A_sample_that_cannot_be_read_shows_a_slovak_message()
    {
        var broken = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
        File.WriteAllBytes(broken, [1, 2, 3]);
        try
        {
            var main = new MainViewModel([new SampleItem("broken", "Pokazený", broken)], CategorizationConfig.Default);

            Assert.Equal(Texts.StatementUnreadable, main.Error);
        }
        finally
        {
            File.Delete(broken);
        }
    }

    [Fact]
    public void Without_samples_nothing_is_shown()
    {
        var main = new MainViewModel([], CategorizationConfig.Default);

        Assert.Null(main.SelectedSample);
        Assert.Empty(main.Statement.Categories);
    }
}
