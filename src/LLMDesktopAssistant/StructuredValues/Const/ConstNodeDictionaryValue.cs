using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Const
{
	/// <summary>
	/// An immutable structured dictionary value.
	/// </summary>
	[JsonDerived(typeof(ConstNodeValue), "dictionary")]
	public sealed class ConstNodeDictionaryValue : ConstNodeValue, INodeDictionaryValue
	{
		/// <summary>
		/// Gets the entries of the dictionary.
		/// </summary>
		public ImmutableDictionary<string, ConstNodeValue> Items { get; init; } = [];

		IReadOnlyDictionary<string, INodeValue> INodeDictionaryValue.Items =>
			Items.ToDictionary(v => v.Key, v => (INodeValue)v.Value);

		/// <inheritdoc />
		public override object? TakeValueSnapshot()
		{
			return Items.Select(v => KeyValuePair.Create(v.Key, v.Value.TakeValueSnapshot())).ToArray();
		}
	}
}
