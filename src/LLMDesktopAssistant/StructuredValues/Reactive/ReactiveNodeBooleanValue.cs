using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Reactive
{
	/// <summary>
	/// A reactive structured boolean value.
	/// </summary>
	[JsonDerived(typeof(ReactiveNodeValue), "boolean")]
	public class ReactiveNodeBooleanValue : ReactiveNodeValue, INodeBooleanValue
	{
		/// <summary>
		/// Gets or sets the boolean value.
		/// </summary>
		public bool Value
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <inheritdoc />
		public override object? TakeValueSnapshot()
		{
			return Value;
		}
	}
}
