using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Const
{
	/// <summary>
	/// An immutable structured null value.
	/// </summary>
	[JsonDerived(typeof(ConstNodeValue), "null")]
	public sealed class ConstNodeNullValue : ConstNodeValue, INodeNullValue
	{
		/// <summary>
		/// Gets the singleton instance of the null value.
		/// </summary>
		public static ConstNodeNullValue Instance { get; } = new();

		/// <inheritdoc />
		public override object? TakeValueSnapshot()
		{
			return null;
		}
	}
}
