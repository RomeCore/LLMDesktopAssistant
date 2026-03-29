using System.Windows.Media;

namespace LLMDesktopAssistant.Converters
{
	public sealed class BooleanToBrushConverter : BooleanConverter<Brush>
	{
		public BooleanToBrushConverter() :
			base(Brushes.White, Brushes.Transparent)
		{ }
	}
}