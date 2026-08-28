using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;
using LLTSharp;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Values
{
	[JsonDerived(typeof(ParameterSchemaValue), "dictionary")]
	public class ParameterSchemaDictionaryValue : ParameterSchemaValue
	{
		private readonly ObservableDictionary<string, ParameterSchemaValue> _items = [];
		public ObservableDictionary<string, ParameterSchemaValue> Items
		{
			get => _items;
			set => _items.Reset(value);
		}

		public override object? TakeValueSnapshot()
		{
			return Items.Select(v => KeyValuePair.Create(v.Key, v.Value.TakeValueSnapshot())).ToArray();
		}

		public override TemplateDataAccessor GetTemplateDataAccessor()
		{
			return new TemplateDictionaryAccessor(Items.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetTemplateDataAccessor()));
		}
	}
}
