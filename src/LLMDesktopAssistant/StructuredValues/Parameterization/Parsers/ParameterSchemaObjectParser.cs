using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Parsers
{
	[Service(typeof(IParameterSchemaParser))]
	public class ParameterSchemaObjectParser(
		IParameterSchemaParserManager parserManager
	) : IParameterSchemaParser
	{
		public string ElementType => "object";

		public ParameterSchemaLimitationType SupportedValueTypes => ParameterSchemaLimitationType.NotSpecified;

		public ParameterSchemaElement Parse(INodeDictionaryValue schema,
			ParameterSchemaLimitationType requestedType, ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			var propertiesPath = path.Append("properties");

			if (!schema.Items.TryGetValue("properties", out var properties))
			{
				return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
				{
					Path = propertiesPath,
					Type = ParameterSchemaParsingErrorType.MissingProperty,
					Message = "The 'properties' property is missing."
				}, errors);
			}

			if (properties is not INodeDictionaryValue propertiesDict)
			{
				return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
				{
					Path = propertiesPath,
					Type = ParameterSchemaParsingErrorType.InvalidPropertyType,
					Message = "The 'properties' property must be a dictionary."
				}, errors);
			}

			var objectSchemaElement = new ParameterSchemaObjectElement();

			foreach (var (key, value) in propertiesDict.Items)
			{
				objectSchemaElement.Properties.Add(key, parserManager.Parse(value, propertiesPath.Append(key), errors));
			}

			return ParameterSchemaParserHelpers.ApplyCommonProperties(objectSchemaElement, schema, path, errors) ?? objectSchemaElement;
		}
	}
}
