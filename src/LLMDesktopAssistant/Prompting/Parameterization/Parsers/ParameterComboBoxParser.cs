using LLMDesktopAssistant.Prompting.Parameterization.Elements;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using LLTSharp;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Parsers
{
	[Service(typeof(IParameterSchemaParser))]
	public class ParameterComboBoxParser : IParameterSchemaParser
	{
		public string ElementType => "combobox";

		public ParameterSchemaLimitationType SupportedValueTypes => ParameterSchemaLimitationType.String | ParameterSchemaLimitationType.Boolean;

		public ParameterSchemaElement Parse(TemplateDictionaryAccessor lltSchema,
			ParameterSchemaLimitationType requestedType, ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			if (requestedType == ParameterSchemaLimitationType.Boolean)
				return ParseBoolean(lltSchema, path, errors);

			return ParseString(lltSchema, path, errors);
		}

		private static ParameterSchemaElement ParseBoolean(TemplateDictionaryAccessor lltSchema,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			var def = false;
			string? trueTitle = null, falseTitle = null;

			if (ParameterSchemaParserHelpers.OptionalBoolean(lltSchema, "default", path, errors, out var defOpt) is { } errDef)
				return errDef;
			if (defOpt.HasValue)
				def = defOpt.Value;

			if (ParameterSchemaParserHelpers.OptionalString(lltSchema, "trueTitle", path, errors, out trueTitle) is { } errTrue)
				return errTrue;
			if (ParameterSchemaParserHelpers.OptionalString(lltSchema, "falseTitle", path, errors, out falseTitle) is { } errFalse)
				return errFalse;

			var element = new ParameterComboBoxElement
			{
				ValueType = ParameterSchemaLimitationType.Boolean,
				DefaultBoolean = def,
				TrueTitle = trueTitle,
				FalseTitle = falseTitle
			};
			return ParameterSchemaParserHelpers.ApplyCommonProperties(element, lltSchema, path, errors) ?? element;
		}

		private static ParameterSchemaElement ParseString(TemplateDictionaryAccessor lltSchema,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			string? def = null;
			var isEditable = false;
			List<string>? choices = null;

			if (ParameterSchemaParserHelpers.OptionalString(lltSchema, "default", path, errors, out def) is { } errDef)
				return errDef;
			if (ParameterSchemaParserHelpers.OptionalBoolean(lltSchema, "isEditable", path, errors, out var editableOpt) is { } errEditable)
				return errEditable;
			if (editableOpt.HasValue)
				isEditable = editableOpt.Value;

			if (ParameterSchemaParserHelpers.OptionalArray(lltSchema, "choices", path, errors, out var choicesArr) is { } errChoices)
				return errChoices;
			if (choicesArr is not null)
			{
				choices = [];
				var choicesPath = path.Append("choices");
				foreach (var item in choicesArr)
				{
					if (item is not TemplateStringAccessor str)
					{
						return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
						{
							Path = choicesPath,
							Type = ParameterSchemaParsingErrorType.InvalidPropertyType,
							Message = "Each item of the 'choices' array must be a string."
						}, errors);
					}
					choices.Add(str.Value);
				}
			}

			if (def is not null && choices is not null && !choices.Contains(def) && !isEditable)
				return ParameterSchemaParserHelpers.InvalidPropertyValue(path, errors,
					$"The 'default' value '{def}' is not present in the 'choices' list.");

			var element = new ParameterComboBoxElement
			{
				ValueType = ParameterSchemaLimitationType.String,
				Choices = choices,
				IsEditable = isEditable,
				Default = def
			};
			return ParameterSchemaParserHelpers.ApplyCommonProperties(element, lltSchema, path, errors) ?? element;
		}
	}
}
