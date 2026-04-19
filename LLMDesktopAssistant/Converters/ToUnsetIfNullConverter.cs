using Avalonia;
using Avalonia.Data.Converters;

namespace LLMDesktopAssistant.Converters
{
	public sealed class ToUnsetIfNullConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		{
			return value ?? AvaloniaProperty.UnsetValue;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}