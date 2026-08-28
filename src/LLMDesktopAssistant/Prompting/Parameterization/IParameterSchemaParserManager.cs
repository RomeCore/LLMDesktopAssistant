using LLMDesktopAssistant.Prompting.Parameterization.Elements;
using LLMDesktopAssistant.Utils;
using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Parameterization
{
	public interface IParameterSchemaParserManager
	{
		ParameterSchemaElement Parse(TemplateDataAccessor lltSchema,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors);

		ParameterSchema ParseRoot(TemplateDataAccessor lltSchema, AppendOnlyList<ParameterSchemaParsingError> errors);
	}
}
