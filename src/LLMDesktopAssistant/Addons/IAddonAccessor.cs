using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Addons
{
	/// <summary>
	/// The addon accessor used for accessing the list of addons of a specific type.
	/// Contains the reactive list of addons.
	/// </summary>
	public interface IAddonAccessor<T>
	{
		/// <summary>
		/// Gets the list of all addons of the specified type.
		/// </summary>
		ReadOnlyObservableCollection<T> Addons { get; }
	}
}
