using LLMDesktopAssistant.Utils.Json;
using LLTSharp;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Values
{
	[JsonDerived(typeof(ParameterSchemaValue), "number")]
	public class ParameterSchemaNumberValue : ParameterSchemaValue
	{
		public double Value
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
			return new TemplateNumberAccessor(Value);
		}
	}
}
