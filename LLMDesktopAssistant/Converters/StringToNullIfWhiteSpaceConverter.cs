using Avalonia.Data.Converters;
using System.Globalization;

namespace LLMDesktopAssistant.Converters
{
	public class StringToNullIfWhiteSpaceConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return string.IsNullOrWhiteSpace(value as string) ? null : value;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}