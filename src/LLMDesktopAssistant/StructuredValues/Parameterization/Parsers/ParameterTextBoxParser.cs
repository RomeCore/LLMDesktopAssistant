using System.Globalization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.Utils;
namespace LLMDesktopAssistant.StructuredValues.Parameterization.Parsers
{
	[Service(typeof(IParameterSchemaParser))]
	public class ParameterTextBoxParser : IParameterSchemaParser
	{
		public string ElementType => "textbox";

		public ParameterSchemaLimitationType SupportedValueTypes => ParameterSchemaLimitationType.String | ParameterSchemaLimitationType.Number;

		public ParameterSchemaElement Parse(INodeDictionaryValue schema,
			ParameterSchemaLimitationType requestedType, ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			var isMultiline = false;
			string? def = null, placeholder = null;

			if (ParameterSchemaParserHelpers.OptionalBoolean(schema, "isMultiline", path, errors, out var multilineOpt) is { } errMultiline)
				return errMultiline;
			if (multilineOpt.HasValue)
				isMultiline = multilineOpt.Value;

			if (ParameterSchemaParserHelpers.OptionalString(schema, "placeholder", path, errors, out placeholder) is { } errPlaceholder)
				return errPlaceholder;

			if (schema.Items.TryGetValue("default", out var defAccessor))
			{
				switch (defAccessor)
				{
					case INodeStringValue str:
						def = str.Value;
						break;

					case INodeNumberValue num when requestedType == ParameterSchemaLimitationType.Number:
						def = num.Value.ToString(CultureInfo.InvariantCulture);
						break;

					default:
						return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
						{
							Path = path.Append("default"),
							Type = ParameterSchemaParsingErrorType.InvalidPropertyType,
							Message = $"The 'default' property must be a {(requestedType == ParameterSchemaLimitationType.Number ? "number" : "string")}, but found '{ParameterSchemaParserHelpers.GetNodeTypeName(defAccessor)}'."
						}, errors);
				}
			}

			var element = new ParameterTextBoxElement
			{
				ValueType = requestedType,
				IsMultiline = isMultiline,
				Default = def,
				Placeholder = placeholder
			};
			return ParameterSchemaParserHelpers.ApplyCommonProperties(element, schema, path, errors) ?? element;
		}
	}
}
