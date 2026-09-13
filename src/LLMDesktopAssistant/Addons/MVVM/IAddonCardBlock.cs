using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Addons.MVVM
{
	public interface IAddonCardBlock : IAddonCardElement
	{
		/// <summary>
		/// The title that will be shown on top of the content of this block.
		/// </summary>
		LocaleKeyBase? Title { get; }

		/// <summary>
		/// Whether to show this block in the 'details' section.
		/// </summary>
		public bool IsDetail { get; }

		/// <summary>
		/// The content that will be shown inside this block.
		/// </summary>
		object? Content { get; }

		/// <summary>
		/// The chips that will be shown below the content of this block.
		/// </summary>
		ImmutableList<AddonCardChip>? Chips { get; }
	}
}
