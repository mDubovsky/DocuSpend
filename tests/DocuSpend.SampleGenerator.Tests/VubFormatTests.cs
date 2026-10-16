using DocuSpend.SampleGenerator.Rendering;

namespace DocuSpend.SampleGenerator.Tests;

public class VubFormatTests
{
    [Theory]
    [InlineData("0", "0,00")]
    [InlineData("7", "7,00")]
    [InlineData("-7", "-7,00")]
    [InlineData("1100", "1.100,00")]
    [InlineData("-1893.52", "-1.893,52")]
    [InlineData("1234567.8", "1.234.567,80")]
    public void Amounts_use_dot_thousands_and_comma_decimals(string value, string expected) =>
        Assert.Equal(expected, VubFormat.Amount(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Dates_in_the_table_have_no_year() =>
        Assert.Equal("04/09", VubFormat.TableDate(new DateOnly(2026, 9, 4)));

    [Fact]
    public void Full_dates_use_dots() =>
        Assert.Equal("31.08.2026", VubFormat.FullDate(new DateOnly(2026, 8, 31)));
}
