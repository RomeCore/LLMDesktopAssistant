namespace LLMDesktopAssistant.Addons.MVVM
{
	public interface IAddonCardChange : IAddonCardElement
	{
		/// <summary>
		/// Gets the content to show in the header of the addon card.
		/// This is typically a view model that represents the element in header used to apply change.
		/// </summary>
		object? Content { get; }

		/// <summary>
		/// Gets a value indicating whether the <see cref="Content"/> should be shown on the left side of the header of the addon card.
		/// </summary>
		bool IsShownLeft { get; }

		/// <summary>
		/// Removes the change of the addon config instance.
		/// </summary>
		void Reset();
	}
}
