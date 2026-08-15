using System.Globalization;

namespace LLMDesktopAssistant.Converters
{
	/// <summary>
	/// Converts a diff line kind character (' ', '+', '-') to a foreground brush for the line kind indicator.
	/// '+' lines get green, '-' lines get red, ' ' lines get default opacity.
	/// </summary>
	public class DiffLineForegroundConverter : Avalonia.Data.Converters.IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is char kind)
			{
				return kind switch
				{
					'+' => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 76, 175, 80)),   // green
					'-' => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 244, 67, 54)),   // red
					_ => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(128, 128, 128, 128))    // gray
				};
			}
			return Avalonia.Media.Brushes.Gray;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
