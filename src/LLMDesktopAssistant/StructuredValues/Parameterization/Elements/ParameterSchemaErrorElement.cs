using Avalonia.Controls;
using LLMDesktopAssistant.StructuredValues.Reactive;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Elements
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

		public override ReactiveNodeValue CreateOrFixValue(ReactiveNodeValue? existing, AppendOnlyList<ParameterValidationLogEntry> log)
		{
			return new ReactiveNodeNullValue();
		}

		public override Control CreateControl(ReactiveNodeValue value)
		{
			return new Panel();
		}
	}
}
