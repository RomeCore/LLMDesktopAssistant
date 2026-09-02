using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Parsers
{
	/// <summary>
	/// Helper methods for reading typed fields from a parameter schema dictionary.
	/// Every method returns a <see cref="ParameterSchemaErrorElement"/> (and appends the error)
	/// when the field is missing or has an invalid type, or <c>null</c> on success.
	/// </summary>
	public static class ParameterSchemaParserHelpers
	{
		public static ParameterSchemaErrorElement? RequireString(INodeDictionaryValue schema, string key,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors, out string value)
		{
			value = null!;
			if (!schema.Items.TryGetValue(key, out var accessor))
				return MissingProperty(key, path, errors);
			if (accessor is INodeStringValue str)
			{
				value = str.Value!;
				return null;
			}
			return InvalidPropertyType(key, path, errors, "string", accessor);
		}

		public static ParameterSchemaErrorElement? OptionalString(INodeDictionaryValue schema, string key,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors, out string? value)
		{
			value = null;
			if (!schema.Items.TryGetValue(key, out var accessor))
				return null;
			if (accessor is INodeStringValue str)
			{
				value = str.Value;
				return null;
			}
			return InvalidPropertyType(key, path, errors, "string", accessor);
		}

		public static ParameterSchemaErrorElement? RequireNumber(INodeDictionaryValue schema, string key,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors, out double value)
		{
			value = 0;
			if (!schema.Items.TryGetValue(key, out var accessor))
				return MissingProperty(key, path, errors);
			if (accessor is INodeNumberValue num)
			{
				value = num.Value;
				return null;
			}
			return InvalidPropertyType(key, path, errors, "number", accessor);
		}

		public static ParameterSchemaErrorElement? OptionalNumber(INodeDictionaryValue schema, string key,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors, out double? value)
		{
			value = null;
			if (!schema.Items.TryGetValue(key, out var accessor))
				return null;
			if (accessor is INodeNumberValue num)
			{
				value = num.Value;
				return null;
			}
			return InvalidPropertyType(key, path, errors, "number", accessor);
		}

		public static ParameterSchemaErrorElement? OptionalBoolean(INodeDictionaryValue schema, string key,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors, out bool? value)
		{
			value = null;
			if (!schema.Items.TryGetValue(key, out var accessor))
				return null;
			if (accessor is INodeBooleanValue boolean)
			{
				value = boolean.Value;
				return null;
			}
			return InvalidPropertyType(key, path, errors, "boolean", accessor);
		}

		public static ParameterSchemaErrorElement? OptionalArray(INodeDictionaryValue schema, string key,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors, out IEnumerable<INodeValue>? value)
		{
			value = null;
			if (!schema.Items.TryGetValue(key, out var accessor))
				return null;
			if (accessor is INodeArrayValue array)
			{
				value = array.Items;
				return null;
			}
			return InvalidPropertyType(key, path, errors, "array", accessor);
		}

		/// <summary>
		/// Reads the common 'title' and 'description' properties and applies them to the element.
		/// </summary>
		public static ParameterSchemaElement? ApplyCommonProperties(ParameterSchemaElement element,
			INodeDictionaryValue schema, ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
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

		/// <summary>
		/// Gets the semantic type name of the given node value (e.g. <c>"string"</c>, <c>"dictionary"</c>).
		/// Used for diagnostics.
		/// </summary>
		public static string GetNodeTypeName(INodeValue value) => value switch
		{
			INodeNullValue => "null",
			INodeBooleanValue => "boolean",
			INodeNumberValue => "number",
			INodeStringValue => "string",
			INodeArrayValue => "array",
			INodeDictionaryValue => "dictionary",
			_ => value.GetType().Name
		};

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
			AppendOnlyList<ParameterSchemaParsingError> errors, string expected, INodeValue accessor)
		{
			return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
			{
				Path = path.Append(key),
				Type = ParameterSchemaParsingErrorType.InvalidPropertyType,
				Message = $"The '{key}' property must be a {expected}, but found '{GetNodeTypeName(accessor)}'."
			}, errors);
		}
	}
}
