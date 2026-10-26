namespace DocuSpend.Presentation.Tests;

/// <summary>Which day of the balance line a pointer is on (spec: the balance chart's card).</summary>
public class BalanceDaysTests
{
    private static readonly DateOnly Start = new(2026, 5, 1);
    private static readonly DateOnly End = new(2026, 5, 31);

    [Theory]
    [InlineData(2026, 5, 14, 0, 0, 14)]
    [InlineData(2026, 5, 14, 12, 0, 14)]
    [InlineData(2026, 5, 14, 23, 59, 14)]
    [InlineData(2026, 5, 15, 0, 0, 15)]
    public void The_day_is_the_one_whose_stretch_of_the_line_the_pointer_is_on(int year, int month, int day, int hour, int minute, int expected) =>
        Assert.Equal(new DateOnly(2026, 5, expected), BalanceDays.At(new DateTime(year, month, day, hour, minute, 0), Start, End));

    [Theory]
    [InlineData(2026, 4, 28)]
    [InlineData(2026, 5, 1)]
    public void A_pointer_before_the_first_day_belongs_to_the_first_day(int year, int month, int day) =>
        Assert.Equal(Start, BalanceDays.At(new DateTime(year, month, day, 13, 50, 0), Start, End));

    [Theory]
    [InlineData(2026, 5, 31)]
    [InlineData(2026, 6, 2)]
    public void A_pointer_after_the_last_day_belongs_to_the_last_day(int year, int month, int day) =>
        Assert.Equal(End, BalanceDays.At(new DateTime(year, month, day, 10, 9, 0), Start, End));
}
