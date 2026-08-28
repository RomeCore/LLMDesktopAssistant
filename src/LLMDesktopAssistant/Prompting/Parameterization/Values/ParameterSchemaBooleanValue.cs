using LLMDesktopAssistant.Utils.Json;
using LLTSharp;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Values
{
	[JsonDerived(typeof(ParameterSchemaValue), "boolean")]
	public class ParameterSchemaBooleanValue : ParameterSchemaValue
	{
		public bool Value
		{
			get;
			set => SetProperty(ref field, value);
		}

		public override object? TakeValueSnapshot()
		{
			return Value;
		}

		public override TemplateDataAccessor GetTemplateDataAccessor()
		{
			return new TemplateBooleanAccessor(Value);
		}
	}
}
