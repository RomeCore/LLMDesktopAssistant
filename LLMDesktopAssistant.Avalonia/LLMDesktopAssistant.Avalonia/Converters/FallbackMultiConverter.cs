using Avalonia.Data.Converters;
using System.Globalization;

namespace LLMDesktopAssistant.Avalonia.Converters
{
	public class FallbackMultiConverter : IMultiValueConverter
	{
		public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
		{
			for (int i = 0; i < values.Count; i++)
				if (values[i] != null)
					return values[i];
			return null!;
		}

		public IList<object?> ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}