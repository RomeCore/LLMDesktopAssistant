using Avalonia.Data.Converters;

namespace LLMDesktopAssistant.Avalonia.Converters
{
	public sealed class NotnullToBooleanConverter : IValueConverter
	{
		public bool Invert { get; set; } = false;

		public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		{
			if (Invert)
				return value == null ? true : false;
			return value != null ? true : false;
		}

		public object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		{
			throw new System.NotImplementedException();
		}
	}
}