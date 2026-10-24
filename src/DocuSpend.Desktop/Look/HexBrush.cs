using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DocuSpend.Desktop.Look;

/// <summary>A palette colour ("#RRGGBB", from DocuSpend.Presentation) as a brush, for the legend's colour dots.</summary>
internal sealed class HexBrush : IValueConverter
{
    public static HexBrush Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string hex ? new SolidColorBrush(Color.Parse(hex)) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
