using System.Windows.Media;

namespace LLMDesktopAssistant.Core.Converters
{
	public sealed class BooleanToBrushConverter : BooleanConverter<Brush>
	{
		public BooleanToBrushConverter() :
			base(Brushes.White, Brushes.Transparent)
		{ }
	}
}