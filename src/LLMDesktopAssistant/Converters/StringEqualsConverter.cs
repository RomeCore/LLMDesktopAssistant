using System.Globalization;
using Avalonia.Data.Converters;

namespace LLMDesktopAssistant.Converters;

/// <summary>
/// Converts a string value by comparing it to the converter parameter.
/// Returns true if the string equals the parameter (case-insensitive by default).
/// </summary>
public class StringEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string strValue && parameter is string strParam)
        {
            return strValue.Equals(strParam, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
