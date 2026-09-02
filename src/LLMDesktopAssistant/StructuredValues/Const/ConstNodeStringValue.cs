using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Const
{
	/// <summary>
	/// An immutable structured string value.
	/// </summary>
	[JsonDerived(typeof(ConstNodeValue), "string")]
	public sealed class ConstNodeStringValue : ConstNodeValue, INodeStringValue
	{
		/// <summary>
		/// Gets the string value.
		/// </summary>
		public required string? Value { get; init; }

		/// <inheritdoc />
		public override object? TakeValueSnapshot()
		{
			return Value;
		}
	}
}
