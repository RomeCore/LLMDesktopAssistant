using LLMDesktopAssistant.Prompting.Parameterization.Elements;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using LLTSharp;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Parsers
{
	[Service(typeof(IParameterSchemaParser))]
	public class ParameterListParser : IParameterSchemaParser
	{
		private readonly IParameterSchemaParserManager _parserManager;

		public ParameterListParser(IParameterSchemaParserManager parserManager)
		{
			_parserManager = parserManager;
		}

		public string ElementType => "list";

		public ParameterSchemaLimitationType SupportedValueTypes => ParameterSchemaLimitationType.NotSpecified;

		public ParameterSchemaElement Parse(TemplateDictionaryAccessor lltSchema,
			ParameterSchemaLimitationType requestedType, ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			var itemsPath = path.Append("items");
			if (!lltSchema.Dictionary.TryGetValue("items", out var itemsAccessor))
			{
				return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
				{
					Path = itemsPath,
					Type = ParameterSchemaParsingErrorType.MissingProperty,
					Message = "The 'items' property is missing."
				}, errors);
			}
			var itemsSchema = _parserManager.Parse(itemsAccessor, itemsPath, errors);

			var min = 0;
			var max = int.MaxValue;
			if (ParameterSchemaParserHelpers.OptionalNumber(lltSchema, "min", path, errors, out var minOpt) is { } errMin)
				return errMin;
			if (minOpt.HasValue)
				min = (int)Math.Round(minOpt.Value);
			if (ParameterSchemaParserHelpers.OptionalNumber(lltSchema, "max", path, errors, out var maxOpt) is { } errMax)
				return errMax;
			if (maxOpt.HasValue)
				max = (int)Math.Round(maxOpt.Value);

			if (min < 0 || max < 0 || min > max)
				return ParameterSchemaParserHelpers.InvalidPropertyValue(path, errors,
					$"The 'min' ({min}) and 'max' ({max}) values must be non-negative and 'min' must be less than or equal to 'max'.");

			var element = new ParameterListElement
			{
				ItemsSchema = itemsSchema,
				Min = min,
				Max = max
			};
			return ParameterSchemaParserHelpers.ApplyCommonProperties(element, lltSchema, path, errors) ?? element;
		}
	}
}
