using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Reactive
{
	/// <summary>
	/// A reactive structured null value.
	/// </summary>
	[JsonDerived(typeof(ReactiveNodeValue), "null")]
	public class ReactiveNodeNullValue : ReactiveNodeValue, INodeNullValue
	{
		/// <inheritdoc />
		public override object? TakeValueSnapshot()
		{
			return null;
		}
	}
}
