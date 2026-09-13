namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// The base implementation of <see cref="IAddonCardElement"/>: provides ordering and change
	/// notification (the latter is required by stateful elements, such as collapsible blocks).
	/// </summary>
	public abstract class AddonCardElementBase : NotifyPropertyChanged, IAddonCardElement
	{
		/// <inheritdoc/>
		public int Order { get; init; }
	}
}
