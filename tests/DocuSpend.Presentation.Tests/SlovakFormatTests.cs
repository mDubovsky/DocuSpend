using System.Globalization;

namespace DocuSpend.Presentation.Tests;

/// <summary>Amounts, dates and typed numbers are Slovak whatever the thread's culture is (spec: Slovak).</summary>
public class SlovakFormatTests
{
    /// <summary>
    /// The Slovak group separator and the space before € are no-break spaces, whose exact character depends on the
    /// culture data of the platform; compare them as plain spaces.
    /// </summary>
    private static string Plain(string text) => text.Replace(' ', ' ').Replace(' ', ' ');

    [Theory]
    [InlineData(1349.87, "1 349,87 €")]
    [InlineData(-640, "-640,00 €")]
    [InlineData(0, "0,00 €")]
    [InlineData(12.5, "12,50 €")]
    public void Amounts_are_formatted_the_slovak_way(decimal amount, string expected)
    {
        Assert.Equal(expected, Plain(SlovakFormat.Amount(amount)));
    }

    [Fact]
    public void Dates_and_periods_are_formatted_the_slovak_way()
    {
        Assert.Equal("24. 10. 2026", Plain(SlovakFormat.Date(new DateOnly(2026, 10, 24))));
        Assert.Equal("1. 3. 2026 – 31. 3. 2026", Plain(SlovakFormat.Period(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31))));
    }

    [Fact]
    public void Shares_are_formatted_the_slovak_way()
    {
        Assert.Equal("47,4 %", Plain(SlovakFormat.Share(0.4742m)));
    }

    [Theory]
    [InlineData("12,50", 12.50)]
    [InlineData("1 349,87", 1349.87)]
    [InlineData("1 349,87", 1349.87)]
    [InlineData(" 7 ", 7)]
    [InlineData("12,50 €", 12.50)]
    public void Typed_amounts_are_read_the_slovak_way(string text, decimal expected)
    {
        Assert.True(SlovakFormat.TryParseAmount(text, out var amount));
        Assert.Equal(expected, amount);
    }

    [Theory]
    [InlineData("12.50")]
    [InlineData("dvanásť")]
    [InlineData("")]
    [InlineData("1,2,3")]
    public void Text_that_is_not_a_slovak_amount_is_refused(string text)
    {
        Assert.False(SlovakFormat.TryParseAmount(text, out _));
    }

    [Fact]
    public void An_english_thread_culture_changes_nothing()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            Assert.Equal("1 349,87 €", Plain(SlovakFormat.Amount(1349.87m)));
            Assert.Equal("24. 10. 2026", Plain(SlovakFormat.Date(new DateOnly(2026, 10, 24))));
            Assert.True(SlovakFormat.TryParseAmount("12,50", out var amount));
            Assert.Equal(12.50m, amount);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
            CultureInfo.CurrentUICulture = before;
        }
    }
}
