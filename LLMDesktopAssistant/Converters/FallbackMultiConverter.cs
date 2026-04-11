using System.Globalization;
using System.Windows.Data;

namespace LLMDesktopAssistant.Core.Converters
{
	public class FallbackMultiConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			for (int i = 0; i < values.Length; i++)
				if (values[i] != null)
					return values[i];
			return null!;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}