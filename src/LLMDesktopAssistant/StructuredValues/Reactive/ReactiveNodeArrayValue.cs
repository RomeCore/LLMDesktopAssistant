using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Reactive
{
	/// <summary>
	/// A reactive structured array value.
	/// </summary>
	[JsonDerived(typeof(ReactiveNodeValue), "array")]
	public class ReactiveNodeArrayValue : ReactiveNodeValue, INodeArrayValue
	{
		private readonly RangeObservableCollection<ReactiveNodeValue> _items = [];

		/// <summary>
		/// Gets or sets the items of the array.
		/// </summary>
		public RangeObservableCollection<ReactiveNodeValue> Items
		{
			get => _items;
			set => _items.Reset(value);
		}

		IReadOnlyList<INodeValue> INodeArrayValue.Items => Items;

		/// <inheritdoc />
		public override object? TakeValueSnapshot()
		{
			return Items.Select(v => v.TakeValueSnapshot()).ToArray();
		}
	}
}
