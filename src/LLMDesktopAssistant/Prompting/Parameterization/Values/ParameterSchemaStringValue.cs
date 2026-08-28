using LLMDesktopAssistant.Utils.Json;
using LLTSharp;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Values
{
	[JsonDerived(typeof(ParameterSchemaValue), "string")]
	public class ParameterSchemaStringValue : ParameterSchemaValue
	{
		public string? Value
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
			return new TemplateStringAccessor(Value);
		}
	}
}
