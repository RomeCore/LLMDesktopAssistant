using LLMDesktopAssistant.Prompting.Parameterization.Elements;
using LLMDesktopAssistant.Utils;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Parsers
{
	public interface IParameterSchemaParser
	{
		string ElementType { get; }

		ParameterSchemaLimitationType SupportedValueTypes { get; }

		ParameterSchemaElement Parse(TemplateDictionaryAccessor lltSchema, ParameterSchemaLimitationType requestedType,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors);
	}
}
