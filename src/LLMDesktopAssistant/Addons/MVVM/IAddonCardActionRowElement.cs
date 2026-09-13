namespace LLMDesktopAssistant.Addons.MVVM
{
	public interface IAddonCardActionRowElement : IAddonCardElement
	{
		/// <summary>
		/// The content that will be shown at the left side of the action row.
		/// </summary>
		object? Content { get; }
	}
}
