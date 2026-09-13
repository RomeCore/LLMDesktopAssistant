using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// A block of the addon card: an optional title, an arbitrary content and optional chips below it.
	/// The <see cref="Visibility"/> determines whether the block is inline, collapsible or a details section part.
	/// </summary>
	public interface IAddonCardBlock : IAddonCardElement
	{
		/// <summary>
		/// The title that will be shown on top of the content of this block.
		/// </summary>
		LocaleKeyBase? Title { get; }

		/// <summary>
		/// Where and when this block is rendered.
		/// </summary>
		AddonCardBlockVisibility Visibility { get; }

		/// <summary>
		/// The content that will be shown inside this block.
		/// </summary>
		object? Content { get; }

		/// <summary>
		/// The chips that will be shown below the content of this block.
		/// </summary>
		ImmutableList<IAddonCardChip>? Chips { get; }

		/// <summary>
		/// The icon of the toggle button that expands this block.
		/// Only used by <see cref="AddonCardBlockVisibility.Collapsible"/> blocks.
		/// </summary>
		MaterialIconKind? ToggleIcon { get; }

		/// <summary>
		/// The tooltip of the toggle button that expands this block.
		/// Only used by <see cref="AddonCardBlockVisibility.Collapsible"/> blocks.
		/// </summary>
		LocaleKeyBase? ToggleToolTip { get; }

		/// <summary>
		/// Whether a <see cref="AddonCardBlockVisibility.Collapsible"/> block is currently expanded.
		/// Ignored by other block kinds.
		/// </summary>
		bool IsExpanded { get; set; }
	}
}
