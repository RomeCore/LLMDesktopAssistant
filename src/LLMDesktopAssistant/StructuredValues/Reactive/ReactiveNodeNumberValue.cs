using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Reactive
{
	/// <summary>
	/// A reactive structured number value.
	/// </summary>
	[JsonDerived(typeof(ReactiveNodeValue), "number")]
	public class ReactiveNodeNumberValue : ReactiveNodeValue, INodeNumberValue
	{
		/// <summary>
		/// Gets or sets the number value.
		/// </summary>
		public double Value
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
