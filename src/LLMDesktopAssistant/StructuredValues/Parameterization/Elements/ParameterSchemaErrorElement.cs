using LLMDesktopAssistant.StructuredValues.Reactive;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Elements
{
	/// <summary>
	/// An element that represents a parameter schema parsing error.
	/// </summary>
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
	}
}
