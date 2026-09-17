namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// A card header element that edits (or indicates) an override of the addon configuration.
	/// See <see cref="IAddonCardChange"/> for the change state and the reset command.
	/// </summary>
	public interface IAddonCardHeaderChange : IAddonCardHeaderElement, IAddonCardChange
	{
	}
}
