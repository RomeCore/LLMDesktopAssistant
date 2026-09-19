namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// Determines where and when a card block is rendered.
	/// </summary>
	public enum AddonCardBlockVisibility
	{
		/// <summary>
		/// The block is always visible, right below the chips row.
		/// </summary>
		Inline,

		/// <summary>
		/// The block is collapsed by default and toggled by its own button in the card action row
		/// (e.g. the skill/sub-agent parameters section). Creating a block with this value sets
		/// <see cref="IAddonCardBlock.IsExpanded"/> to <see langword="false"/> automatically:
		/// collapsible blocks always start collapsed.
		/// </summary>
		Collapsible,

		/// <summary>
		/// The block is a part of the card details section and is shown when the user expands the details.
		/// </summary>
		Details
	}
}
