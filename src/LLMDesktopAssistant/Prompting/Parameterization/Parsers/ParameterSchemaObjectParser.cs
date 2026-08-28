using LLMDesktopAssistant.Prompting.Parameterization.Elements;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Parsers
{
	[Service(typeof(IParameterSchemaParser))]
	public class ParameterSchemaObjectParser(
		IParameterSchemaParserManager parserManager
	) : IParameterSchemaParser
	{
		public string ElementType => "object";

		public ParameterSchemaLimitationType SupportedValueTypes => ParameterSchemaLimitationType.NotSpecified;

		public ParameterSchemaElement Parse(TemplateDictionaryAccessor lltSchema,
			ParameterSchemaLimitationType requestedType, ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			var propertiesPath = path.Append("properties");

			if (!lltSchema.Dictionary.TryGetValue("properties", out var properties))
			{
				return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
				{
					Path = propertiesPath,
					Type = ParameterSchemaParsingErrorType.MissingProperty,
					Message = "The 'properties' property is missing."
				}, errors);
			}

			if (properties is not TemplateDictionaryAccessor propertiesDict)
			{
				return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
				{
					Path = propertiesPath,
					Type = ParameterSchemaParsingErrorType.InvalidPropertyType,
					Message = "The 'properties' property must be a dictionary."
				}, errors);
			}

			var objectSchemaElement = new ParameterSchemaObjectElement();

			foreach (var (key, value) in propertiesDict.Dictionary)
			{
				objectSchemaElement.Properties.Add(key, parserManager.Parse(value, propertiesPath.Append(key), errors));
			}

			return ParameterSchemaParserHelpers.ApplyCommonProperties(objectSchemaElement, lltSchema, path, errors) ?? objectSchemaElement;
		}
	}
}
