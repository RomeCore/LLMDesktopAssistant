using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Const
{
	/// <summary>
	/// An immutable structured boolean value.
	/// </summary>
	[JsonDerived(typeof(ConstNodeValue), "boolean")]
	public sealed class ConstNodeBooleanValue : ConstNodeValue, INodeBooleanValue
	{
		/// <summary>
		/// Gets the boolean value.
		/// </summary>
		public required bool Value { get; init; }

		/// <inheritdoc />
		public override object? TakeValueSnapshot()
		{
			return Value;
		}
	}
}
