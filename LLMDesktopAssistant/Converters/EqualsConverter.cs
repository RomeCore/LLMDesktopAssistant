using Avalonia.Data.Converters;
using System.Globalization;

namespace LLMDesktopAssistant.Converters;

/// <summary>
/// Converts a value by comparing it to the converter parameter.
/// </summary>
public class EqualsConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return Equals(value, parameter);
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
