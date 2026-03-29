using System.Windows;

namespace LLMDesktopAssistant.Converters
{
	public sealed class BooleanToVisibilityConverter : BooleanConverter<Visibility>
	{
		public BooleanToVisibilityConverter() :
			base(Visibility.Visible, Visibility.Collapsed)
		{ }
	}
}