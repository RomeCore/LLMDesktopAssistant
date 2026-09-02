using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Reactive
{
	/// <summary>
	/// A reactive structured dictionary value.
	/// </summary>
	[JsonDerived(typeof(ReactiveNodeValue), "dictionary")]
	public class ReactiveNodeDictionaryValue : ReactiveNodeValue, INodeDictionaryValue
	{
		private readonly ObservableDictionary<string, ReactiveNodeValue> _items = [];

		/// <summary>
		/// Gets or sets the entries of the dictionary.
		/// </summary>
		public ObservableDictionary<string, ReactiveNodeValue> Items
		{
			get => _items;
			set => _items.Reset(value);
		}

		IReadOnlyDictionary<string, INodeValue> INodeDictionaryValue.Items
			=> Items.ToDictionary(kvp => kvp.Key, kvp => (INodeValue)kvp.Value);

		/// <inheritdoc />
		public override object? TakeValueSnapshot()
		{
			return Items.Select(v => KeyValuePair.Create(v.Key, v.Value.TakeValueSnapshot())).ToArray();
		}
	}
}
