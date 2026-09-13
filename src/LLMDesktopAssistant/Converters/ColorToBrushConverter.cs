using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace LLMDesktopAssistant.Converters
{
	/// <summary>
	/// Converts a nullable <see cref="Color"/> into a brush so it can be bound to
	/// <see cref="IBrush"/> properties (e.g. BorderBrush / Foreground).
	/// Null is passed through as null so <c>TargetNullValue</c> can kick in.
	/// </summary>
	public class ColorToBrushConverter : IValueConverter
	{
		public static ColorToBrushConverter Instance { get; } = new ColorToBrushConverter();

		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is Color color ? new ImmutableSolidColorBrush(color) : null;

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is ISolidColorBrush brush ? brush.Color : null;
	}
}
