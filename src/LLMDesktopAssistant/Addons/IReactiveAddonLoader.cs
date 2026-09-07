using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Addons
{
	public interface IReactiveAddonLoader<T>
	{
		/// <summary>
		/// Gets the addons that are currently loaded and effective for the current configuration.
		/// This collection is observable and will update automatically when the addons are reloaded.
		/// </summary>
		ReadOnlyObservableCollection<T> Addons { get; }

		/// <summary>
		/// Reloads the addons with the specified files.
		/// </summary>
		void Reload(IEnumerable<AddonPathInfo> files);
	}
}
