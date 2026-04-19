using Avalonia.Media;

namespace LLMDesktopAssistant.Converters
{
	public sealed class BooleanToBrushConverter : BooleanConverter<IBrush>
	{
		public BooleanToBrushConverter() :
			base(Brushes.White, Brushes.Transparent)
		{ }
	}
}