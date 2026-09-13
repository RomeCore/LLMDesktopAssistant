namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// A header element that edits (or indicates) an override of the addon configuration, and therefore
	/// can be reset back to the addon definition value. All changes of a card are reset at once by the
	/// card's reset command.
	/// </summary>
	public interface IAddonCardChange : IAddonCardHeaderElement
	{
		/// <summary>
		/// Removes the override from the addon configuration, restoring the definition value.
		/// </summary>
		void Reset();
	}
}
