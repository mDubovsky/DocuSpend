namespace DocuSpend.Presentation;

/// <summary>Which day of the balance line a pointer is on.</summary>
public static class BalanceDays
{
    /// <summary>
    /// The day whose stretch of the line holds the pointer's position, given as a date and time. A stepped line holds
    /// a day's balance from that day's start until the next day's, so this is the position's date. The chart's axis
    /// reaches a little beyond the period at both ends; those positions belong to the nearest day of the period.
    /// </summary>
    public static DateOnly At(DateTime position, DateOnly first, DateOnly last)
    {
        var day = DateOnly.FromDateTime(position);
        return day < first ? first : day > last ? last : day;
    }
}
