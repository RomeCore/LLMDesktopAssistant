using System.Windows;
using System.Windows.Data;

namespace LLMDesktopAssistant.Converters
{
	public sealed class ToUnsetIfNullConverter : IValueConverter
	{
		public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value ?? DependencyProperty.UnsetValue;
		}

		public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new System.NotImplementedException();
		}
	}
}