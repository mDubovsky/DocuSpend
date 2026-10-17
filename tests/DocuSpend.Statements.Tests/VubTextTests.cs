using DocuSpend.Statements.Reading.Layout;

namespace DocuSpend.Statements.Tests;

public class VubTextTests
{
    [Theory]
    [InlineData("0,00", "0.00")]
    [InlineData("-7,00", "-7.00")]
    [InlineData("1.100,00", "1100.00")]
    [InlineData("-1.234.567,89", "-1234567.89")]
    public void Amounts_are_read_exactly(string text, string expected) =>
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), VubText.Amount(text));

    [Theory]
    [InlineData("1,5")]
    [InlineData("1100,00")]
    [InlineData("12.34")]
    [InlineData("-48,1X")]
    [InlineData("EUR")]
    public void Anything_else_is_not_an_amount(string text) => Assert.Null(VubText.Amount(text));

    [Fact]
    public void A_date_inside_the_period_takes_the_periods_year() =>
        Assert.Equal(new DateOnly(2026, 3, 14), VubText.TableDate("14/03", new(2026, 3, 1), new(2026, 3, 31)));

    [Fact]
    public void A_value_date_in_december_belongs_to_the_previous_year_on_a_january_statement() =>
        Assert.Equal(new DateOnly(2026, 12, 30), VubText.TableDate("30/12", new(2027, 1, 1), new(2027, 1, 31)));

    [Fact]
    public void The_last_day_of_a_december_statement_stays_in_that_year() =>
        Assert.Equal(new DateOnly(2026, 12, 31), VubText.TableDate("31/12", new(2026, 12, 1), new(2026, 12, 31)));

    [Fact]
    public void A_value_date_in_the_previous_month_keeps_the_year() =>
        Assert.Equal(new DateOnly(2026, 1, 29), VubText.TableDate("29/01", new(2026, 2, 1), new(2026, 2, 28)));

    [Theory]
    [InlineData("31/02")]
    [InlineData("00/05")]
    [InlineData("12/13")]
    [InlineData("1/05")]
    public void Impossible_dates_are_not_dates(string text) =>
        Assert.Null(VubText.TableDate(text, new(2026, 2, 1), new(2026, 2, 28)));
}
