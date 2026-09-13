using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <inheritdoc cref="IAddonCardBlock"/>
	public class AddonCardBlock : AddonCardElementBase, IAddonCardBlock
	{
		/// <inheritdoc/>
		public LocaleKeyBase? Title { get; init; }

		/// <inheritdoc/>
		public AddonCardBlockVisibility Visibility { get; init; } = AddonCardBlockVisibility.Inline;

		/// <inheritdoc/>
		public object? Content { get; init; }

		/// <inheritdoc/>
		public ImmutableList<IAddonCardChip>? Chips { get; init; }

		/// <inheritdoc/>
		public MaterialIconKind? ToggleIcon { get; init; }

		/// <inheritdoc/>
		public LocaleKeyBase? ToggleToolTip { get; init; }

		/// <inheritdoc/>
		public bool IsExpanded
		{
			get;
			set => SetProperty(ref field, value);
		}
	}
}
