using LLMDesktopAssistant.Prompting.Parameterization.Elements;
using LLMDesktopAssistant.Utils;
using LLTSharp;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Parsers
{
	/// <summary>
	/// Helper methods for reading typed fields from a parameter schema dictionary.
	/// Every method returns a <see cref="ParameterSchemaErrorElement"/> (and appends the error)
	/// when the field is missing or has an invalid type, or <c>null</c> on success.
	/// </summary>
	public static class ParameterSchemaParserHelpers
	{
		public static ParameterSchemaErrorElement? RequireString(TemplateDictionaryAccessor schema, string key,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors, out string value)
		{
			value = null!;
			if (!schema.Dictionary.TryGetValue(key, out var accessor))
				return MissingProperty(key, path, errors);
			if (accessor is TemplateStringAccessor str)
			{
				value = str.Value;
				return null;
			}
			return InvalidPropertyType(key, path, errors, "string", accessor);
		}

		public static ParameterSchemaErrorElement? OptionalString(TemplateDictionaryAccessor schema, string key,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors, out string? value)
		{
			value = null;
			if (!schema.Dictionary.TryGetValue(key, out var accessor))
				return null;
			if (accessor is TemplateStringAccessor str)
			{
				value = str.Value;
				return null;
			}
			return InvalidPropertyType(key, path, errors, "string", accessor);
		}

		public static ParameterSchemaErrorElement? RequireNumber(TemplateDictionaryAccessor schema, string key,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors, out double value)
		{
			value = 0;
			if (!schema.Dictionary.TryGetValue(key, out var accessor))
				return MissingProperty(key, path, errors);
			if (accessor is TemplateNumberAccessor num)
			{
				value = num.Value;
				return null;
			}
			return InvalidPropertyType(key, path, errors, "number", accessor);
		}

		public static ParameterSchemaErrorElement? OptionalNumber(TemplateDictionaryAccessor schema, string key,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors, out double? value)
		{
			value = null;
			if (!schema.Dictionary.TryGetValue(key, out var accessor))
				return null;
			if (accessor is TemplateNumberAccessor num)
			{
				value = num.Value;
				return null;
			}
			return InvalidPropertyType(key, path, errors, "number", accessor);
		}

		public static ParameterSchemaErrorElement? OptionalBoolean(TemplateDictionaryAccessor schema, string key,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors, out bool? value)
		{
			value = null;
			if (!schema.Dictionary.TryGetValue(key, out var accessor))
				return null;
			if (accessor is TemplateBooleanAccessor boolean)
			{
				value = boolean.Value;
				return null;
			}
			return InvalidPropertyType(key, path, errors, "boolean", accessor);
		}

		public static ParameterSchemaErrorElement? OptionalArray(TemplateDictionaryAccessor schema, string key,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors, out IEnumerable<TemplateDataAccessor>? value)
		{
			value = null;
			if (!schema.Dictionary.TryGetValue(key, out var accessor))
				return null;
			if (accessor is TemplateArrayAccessor array)
			{
				value = array;
				return null;
			}
			return InvalidPropertyType(key, path, errors, "array", accessor);
		}

		/// <summary>
		/// Reads the common 'title' and 'description' properties and applies them to the element.
		/// </summary>
		public static ParameterSchemaElement? ApplyCommonProperties(ParameterSchemaElement element,
			TemplateDictionaryAccessor schema, ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			if (OptionalString(schema, "title", path, errors, out var title) is { } errTitle)
				return errTitle;
			if (OptionalString(schema, "description", path, errors, out var description) is { } errDescription)
				return errDescription;
			element.Title = title;
			element.Description = description;
			return null;
		}

		public static ParameterSchemaErrorElement InvalidPropertyValue(ParameterSchemaPath path,
			AppendOnlyList<ParameterSchemaParsingError> errors, string message)
		{
			return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
			{
				Path = path,
				Type = ParameterSchemaParsingErrorType.InvalidPropertyValue,
				Message = message
			}, errors);
		}

		private static ParameterSchemaErrorElement MissingProperty(string key, ParameterSchemaPath path,
			AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
			{
				Path = path.Append(key),
				Type = ParameterSchemaParsingErrorType.MissingProperty,
				Message = $"The '{key}' property is missing."
			}, errors);
		}

		private static ParameterSchemaErrorElement InvalidPropertyType(string key, ParameterSchemaPath path,
			AppendOnlyList<ParameterSchemaParsingError> errors, string expected, TemplateDataAccessor accessor)
		{
			return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
			{
				Path = path.Append(key),
				Type = ParameterSchemaParsingErrorType.InvalidPropertyType,
				Message = $"The '{key}' property must be a {expected}, but found '{accessor.Type}'."
			}, errors);
		}
	}
}
