using LLMDesktopAssistant.Utils.Json;
using LLTSharp;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Values
{
	[JsonDerived(typeof(ParameterSchemaValue), "null")]
	public class ParameterSchemaNullValue : ParameterSchemaValue
	{
		public override object? TakeValueSnapshot()
		{
			return null;
		}

		public override TemplateDataAccessor GetTemplateDataAccessor()
		{
			return TemplateNullAccessor.Instance;
		}
	}
}
