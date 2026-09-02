using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Reactive
{
	/// <summary>
	/// A reactive structured string value.
	/// </summary>
	[JsonDerived(typeof(ReactiveNodeValue), "string")]
	public class ReactiveNodeStringValue : ReactiveNodeValue, INodeStringValue
	{
		/// <summary>
		/// Gets or sets the string value.
		/// </summary>
		public string? Value
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
