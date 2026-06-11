using Avalonia.Data.Converters;
using System.Globalization;

namespace LLMDesktopAssistant.Converters
{
	public class ValueReplacerConverter : IValueConverter
	{
		public object? Value { get; set; }

		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return Value;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return Value;
		}
	}
}