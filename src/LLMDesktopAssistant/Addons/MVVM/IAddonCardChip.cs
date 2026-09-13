using Avalonia.Media;
using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.Addons.MVVM
{
	public interface IAddonCardChip : IAddonCardElement
	{
		/// <summary>
		/// Whether the chip has a border.
		/// </summary>
		bool HasBorder { get; }

		/// <summary>
		/// The color of the border and the icon. If null, the default color will be used.
		/// </summary>
		Color? Color { get; }

		/// <summary>
		/// The icon to display on the chip. If null, no icon will be displayed.
		/// </summary>
		MaterialIconKind? Icon { get; }

		/// <summary>
		/// The label to display on the chip after the icon. If null, no label will be displayed.
		/// </summary>
		LocaleKeyBase? Label { get; }

		/// <summary>
		/// The tooltip to display when the user hovers over the chip. If null, no tooltip will be displayed.
		/// </summary>
		LocaleKeyBase? ToolTip { get; }
	}
}