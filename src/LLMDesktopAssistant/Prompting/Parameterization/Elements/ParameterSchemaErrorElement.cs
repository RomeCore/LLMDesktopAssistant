using Avalonia.Controls;
using LLMDesktopAssistant.Prompting.Parameterization.Values;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting.Parameterization.Elements
{
	public class ParameterSchemaErrorElement : ParameterSchemaElement
	{
		public ParameterSchemaParsingError ParsingError { get; }

		public ParameterSchemaErrorElement(ParameterSchemaParsingError parsingError)
		{
			ParsingError = parsingError;
		}

		public ParameterSchemaErrorElement(ParameterSchemaParsingError parsingError, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			ParsingError = parsingError;
			errors.Append(ParsingError);
		}

		public override ParameterSchemaValue CreateOrFixValue(ParameterSchemaValue? existing, AppendOnlyList<ParameterValidationLogEntry> log)
		{
			return new ParameterSchemaNullValue();
		}

		public override Control CreateControl(ParameterSchemaValue value)
		{
			return new Panel();
		}
	}
}
