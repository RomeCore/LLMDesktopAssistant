namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// A block that edits (or indicates) an override of the addon configuration. Unlike the regular
	/// blocks, the card draws a clickable accent marker on its left, which resets the override
	/// back to the addon definition value.
	/// </summary>
	public interface IAddonCardBlockChange : IAddonCardBlock, IAddonCardChange
	{
	}
}
