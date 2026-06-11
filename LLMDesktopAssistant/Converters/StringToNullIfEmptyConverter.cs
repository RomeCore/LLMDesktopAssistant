using Avalonia.Data.Converters;
using System.Globalization;

namespace LLMDesktopAssistant.Converters
{
	public class StringToNullIfEmptyConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return string.IsNullOrEmpty(value as string) ? null : value;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}