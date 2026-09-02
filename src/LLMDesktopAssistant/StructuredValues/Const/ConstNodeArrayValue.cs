using System.Collections.Immutable;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Const
{
	/// <summary>
	/// An immutable structured array value.
	/// </summary>
	[JsonDerived(typeof(ConstNodeValue), "array")]
	public sealed class ConstNodeArrayValue : ConstNodeValue, INodeArrayValue
	{
		/// <summary>
		/// Gets the items of the array.
		/// </summary>
		public ImmutableList<ConstNodeValue> Items { get; init; } = [];

		IReadOnlyList<INodeValue> INodeArrayValue.Items => Items;

		/// <inheritdoc />
		public override object? TakeValueSnapshot()
		{
			return Items.Select(v => v.TakeValueSnapshot()).ToArray();
		}
	}
}
