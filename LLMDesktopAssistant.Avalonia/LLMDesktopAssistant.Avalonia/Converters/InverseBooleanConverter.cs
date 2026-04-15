using Avalonia.Data.Converters;
using System.Globalization;

namespace LLMDesktopAssistant.Avalonia.Converters
{
	public class InverseBooleanConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is bool bValue)
			{
				return !bValue;
			}
			return false;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is bool bValue)
			{
				return !bValue;
			}
			return false;
		}
	}
}