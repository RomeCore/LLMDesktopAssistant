namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// An element of an addon card that edits (or indicates) an override of the addon configuration,
	/// and therefore can be reset back to the addon definition value.
	/// All changes of a card are reset at once by the card's reset command.
	/// </summary>
	public interface IAddonCardChange : IAddonCardElement
	{
		/// <summary>
		/// Whether the element holds an overridden (non-definition) value. When <see langword="true"/>,
		/// the card draws an accent marker next to the content of the element.
		/// </summary>
		bool IsChanged { get; }

		/// <summary>
		/// The command used to reset the override value.
		/// </summary>
		ICommand? ResetCommand { get; }
	}
}
