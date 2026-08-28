using LLMDesktopAssistant.Prompting.Parameterization.Elements;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;
using LLTSharp;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Values
{
	[JsonDerived(typeof(ParameterSchemaValue), "array")]
	public class ParameterSchemaArrayValue : ParameterSchemaValue
	{
		private readonly RangeObservableCollection<ParameterSchemaValue> _items = [];
		public RangeObservableCollection<ParameterSchemaValue> Items
		{
			get => _items;
			set => _items.Reset(value);
		}

		public override object? TakeValueSnapshot()
		{
			return Items.Select(v => v.TakeValueSnapshot()).ToArray();
		}

		public override TemplateDataAccessor GetTemplateDataAccessor()
		{
			return new TemplateArrayAccessor(Items.Select(v => v.GetTemplateDataAccessor()));
		}
	}
}
