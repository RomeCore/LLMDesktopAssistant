using Avalonia.Data.Converters;
using System.Globalization;

namespace LLMDesktopAssistant.Converters
{
	public class StringNonEmptyToBooleanConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is string str)
				return !string.IsNullOrEmpty(str);
			return value;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}