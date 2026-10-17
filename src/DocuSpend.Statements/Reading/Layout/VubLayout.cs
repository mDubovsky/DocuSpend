namespace DocuSpend.Statements.Reading.Layout;

/// <summary>
/// Positions of a VÚB monthly statement, in points from the top-left corner, as measured in
/// work/001-synthetic-statements/layout.md. Real statements and the samples share them.
/// </summary>
internal static class VubLayout
{
    /// <summary>How far a word may sit from its column. Columns are at least 25 pt apart.</summary>
    public const double ColumnTolerance = 2.5;

    /// <summary>
    /// Words whose baselines differ by less than this are on one line. Lines are at least 9 pt apart; a fee-type
    /// pictogram, printed larger in another font, sits a little over 2 pt off its line's baseline.
    /// </summary>
    public const double LineTolerance = 3.0;

    // Account block, on every page.
    public const double AccountLabelX = 45.4;
    public const double AccountValueX = 113.4;
    public const double AccountValueEnd = 330.0;

    // Balance summary, page 1: labels at the left, amounts right-aligned, printed slightly below their label.
    public const double SummaryLabelX = 45.4;
    public const double SummaryValueRight = 215.9;
    public const double SummaryValueMaxDrop = 10.0;

    // Transaction table columns.
    public const double DateX = 45.4;
    public const double ValueDateX = 80.8;
    public const double AccountX = 121.9;
    public const double DescriptionX = 121.4;
    public const double CardX = 221.9;
    public const double VsX = 334.5;
    public const double KsX = 391.2;
    public const double SsX = 419.5;
    public const double FeeTypeX = 479.1;
    public const double FeeX = 487.0;
    public const double AmountRight = 565.9;

    /// <summary>The value date line of a card payment sits about 11 pt below the main line; transactions are at least 26 pt apart.</summary>
    public const double SecondLineMaxGap = 14.0;
}
