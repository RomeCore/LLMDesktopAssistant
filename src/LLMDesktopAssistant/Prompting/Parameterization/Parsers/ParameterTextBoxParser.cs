using System.Globalization;
using LLMDesktopAssistant.Prompting.Parameterization.Elements;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using LLTSharp;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Parsers
{
	[Service(typeof(IParameterSchemaParser))]
	public class ParameterTextBoxParser : IParameterSchemaParser
	{
		public string ElementType => "textbox";

		public ParameterSchemaLimitationType SupportedValueTypes => ParameterSchemaLimitationType.String | ParameterSchemaLimitationType.Number;

		public ParameterSchemaElement Parse(TemplateDictionaryAccessor lltSchema,
			ParameterSchemaLimitationType requestedType, ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			var isMultiline = false;
			string? def = null, placeholder = null;

			if (ParameterSchemaParserHelpers.OptionalBoolean(lltSchema, "isMultiline", path, errors, out var multilineOpt) is { } errMultiline)
				return errMultiline;
			if (multilineOpt.HasValue)
				isMultiline = multilineOpt.Value;

			if (ParameterSchemaParserHelpers.OptionalString(lltSchema, "placeholder", path, errors, out placeholder) is { } errPlaceholder)
				return errPlaceholder;

			if (lltSchema.Dictionary.TryGetValue("default", out var defAccessor))
			{
				switch (defAccessor)
				{
					case TemplateStringAccessor str:
						def = str.Value;
						break;

					case TemplateNumberAccessor num when requestedType == ParameterSchemaLimitationType.Number:
						def = num.Value.ToString(CultureInfo.InvariantCulture);
						break;

					default:
						return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
						{
							Path = path.Append("default"),
							Type = ParameterSchemaParsingErrorType.InvalidPropertyType,
							Message = $"The 'default' property must be a {(requestedType == ParameterSchemaLimitationType.Number ? "number" : "string")}, but found '{defAccessor.Type}'."
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
			return ParameterSchemaParserHelpers.ApplyCommonProperties(element, lltSchema, path, errors) ?? element;
		}
	}
}
