using System.Windows;
using System.Windows.Data;

namespace LLMDesktopAssistant.Converters
{
	public sealed class NotnullToVisibilityConverter : IValueConverter
	{
		public bool Invert { get; set; } = false;

		public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (Invert)
				return value == null ? Visibility.Visible : Visibility.Collapsed;
			return value != null ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new System.NotImplementedException();
		}
	}
}