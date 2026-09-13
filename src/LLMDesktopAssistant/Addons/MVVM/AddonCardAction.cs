using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.Addons.MVVM
{
	public class AddonCardAction : AddonCardElementBase, IAddonCardAction
	{
		/// <inheritdoc/>
		public required MaterialIconKind Icon { get; init; }

		/// <inheritdoc/>
		public required ICommand Command { get; init; }

		/// <inheritdoc/>
		public LocaleKeyBase? ToolTip { get; init; }
	}
}
