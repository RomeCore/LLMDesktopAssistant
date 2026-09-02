using LLTSharp;

namespace LLMDesktopAssistant.StructuredValues.Reactive
{
	public abstract class ReactiveNodeValue : NotifyPropertyChanged, INodeValue
	{
		/// <summary>
		/// Get a snapshot of the current value of the parameter.
		/// This is used mostly for logging validation errors.
		/// </summary>
		/// <returns>A snapshot of the current value of the parameter.</returns>
		public abstract object? TakeValueSnapshot();
	}
}
