using Avalonia.Media;
using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.Addons.MVVM
{
	public class AddonCardChip : AddonCardElementBase, IAddonCardChip
	{
		/// <inheritdoc/>
		public bool HasBorder { get; init; } = true;

		/// <inheritdoc/>
		public Color? Color { get; init; }

		/// <inheritdoc/>
		public MaterialIconKind? Icon { get; init; }

		/// <inheritdoc/>
		public LocaleKeyBase? Label { get; init; }

		/// <inheritdoc/>
		public LocaleKeyBase? ToolTip { get; init; }
	}
}
