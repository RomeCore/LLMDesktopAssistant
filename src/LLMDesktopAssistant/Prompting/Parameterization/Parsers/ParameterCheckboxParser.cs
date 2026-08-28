using LLMDesktopAssistant.Prompting.Parameterization.Elements;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Parsers
{
	[Service(typeof(IParameterSchemaParser))]
	public class ParameterCheckboxParser : IParameterSchemaParser
	{
		public string ElementType => "checkbox";

		public ParameterSchemaLimitationType SupportedValueTypes => ParameterSchemaLimitationType.NotSpecified | ParameterSchemaLimitationType.Boolean;

		public ParameterSchemaElement Parse(TemplateDictionaryAccessor lltSchema,
			ParameterSchemaLimitationType requestedType, ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			var def = false;
			if (ParameterSchemaParserHelpers.OptionalBoolean(lltSchema, "default", path, errors, out var defOpt) is { } errDef)
				return errDef;
			if (defOpt.HasValue)
				def = defOpt.Value;

			var element = new ParameterCheckboxElement
			{
				Default = def
			};
			return ParameterSchemaParserHelpers.ApplyCommonProperties(element, lltSchema, path, errors) ?? element;
		}
	}
}
