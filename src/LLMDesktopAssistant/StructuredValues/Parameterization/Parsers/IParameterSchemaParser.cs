using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Parsers
{
	public interface IParameterSchemaParser
	{
		string ElementType { get; }

		ParameterSchemaLimitationType SupportedValueTypes { get; }

		ParameterSchemaElement Parse(INodeDictionaryValue schema, ParameterSchemaLimitationType requestedType,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors);
	}
}
