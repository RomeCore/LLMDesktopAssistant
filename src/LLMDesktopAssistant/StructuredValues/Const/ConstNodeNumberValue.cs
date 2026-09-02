using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Const
{
	/// <summary>
	/// An immutable structured number value.
	/// </summary>
	[JsonDerived(typeof(ConstNodeValue), "number")]
	public sealed class ConstNodeNumberValue : ConstNodeValue, INodeNumberValue
	{
		/// <summary>
		/// Gets the number value.
		/// </summary>
		public required double Value { get; init; }

		/// <inheritdoc />
		public override object? TakeValueSnapshot()
		{
			return Value;
		}
	}
}
