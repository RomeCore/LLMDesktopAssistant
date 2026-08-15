using System.Globalization;

namespace LLMDesktopAssistant.Converters
{
	/// <summary>
	/// Converts a diff line kind character (' ', '+', '-') to a background brush for the line.
	/// '+' lines get a green tint, '-' lines get a red tint, ' ' lines are transparent.
	/// </summary>
	public class DiffLineBackgroundConverter : Avalonia.Data.Converters.IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is char kind)
			{
				return kind switch
				{
					'+' => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(25, 76, 175, 80)),  // ~15% green
					'-' => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(25, 244, 67, 54)),   // ~15% red
					_ => Avalonia.Media.Brushes.Transparent
				};
			}
			return Avalonia.Media.Brushes.Transparent;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
