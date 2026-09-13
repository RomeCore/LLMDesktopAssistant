using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Addons.MVVM
{
	public class AddonCardBlock : AddonCardElementBase, IAddonCardBlock
	{
		/// <inheritdoc/>
		public LocaleKeyBase? Title { get; init; }

		/// <inheritdoc/>
		public bool IsDetail { get; init; }

		/// <inheritdoc/>
		public object? Content { get; init; }

		/// <inheritdoc/>
		public ImmutableList<AddonCardChip>? Chips { get; init; }
	}
}
