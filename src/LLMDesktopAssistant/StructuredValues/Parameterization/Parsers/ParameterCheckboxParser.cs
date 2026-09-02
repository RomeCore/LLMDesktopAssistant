using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Parsers
{
	[Service(typeof(IParameterSchemaParser))]
	public class ParameterCheckboxParser : IParameterSchemaParser
	{
		public string ElementType => "checkbox";

		public ParameterSchemaLimitationType SupportedValueTypes => ParameterSchemaLimitationType.NotSpecified | ParameterSchemaLimitationType.Boolean;

		public ParameterSchemaElement Parse(INodeDictionaryValue schema,
			ParameterSchemaLimitationType requestedType, ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			var def = false;
			if (ParameterSchemaParserHelpers.OptionalBoolean(schema, "default", path, errors, out var defOpt) is { } errDef)
				return errDef;
			if (defOpt.HasValue)
				def = defOpt.Value;

			var element = new ParameterCheckboxElement
			{
				Default = def
			};
			return ParameterSchemaParserHelpers.ApplyCommonProperties(element, schema, path, errors) ?? element;
		}
	}
}
