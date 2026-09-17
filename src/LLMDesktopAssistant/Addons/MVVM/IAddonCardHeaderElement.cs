namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// An element placed into the addon card header: a slot rendered before (left) or after (right)
	/// of the addon name. Typically hosts interactive controls, such as an enable toggle,
	/// an injection mode selector or a model selector.
	/// </summary>
	public interface IAddonCardHeaderElement : IAddonCardElement
	{
		/// <summary>
		/// The content shown inside the slot. Usually a view model resolved by the view locator.
		/// </summary>
		object? Content { get; }

		/// <summary>
		/// Whether the element is placed to the left of the addon name. Otherwise it is placed to the right.
		/// </summary>
		bool IsShownLeft { get; }
	}
}
