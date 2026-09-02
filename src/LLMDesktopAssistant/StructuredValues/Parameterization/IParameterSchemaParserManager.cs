using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.StructuredValues.Parameterization
{
	public interface IParameterSchemaParserManager
	{
		ParameterSchemaElement Parse(INodeValue schema,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors);

		ParameterSchema ParseRoot(INodeValue schema, AppendOnlyList<ParameterSchemaParsingError> errors);
	}
}
