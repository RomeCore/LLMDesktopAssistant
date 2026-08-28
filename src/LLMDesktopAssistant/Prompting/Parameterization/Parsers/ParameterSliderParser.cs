using LLMDesktopAssistant.Prompting.Parameterization.Elements;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Parameterization.Parsers
{
	[Service(typeof(IParameterSchemaParser))]
	public class ParameterSliderParser : IParameterSchemaParser
	{
		public string ElementType => "slider";

		public ParameterSchemaLimitationType SupportedValueTypes => ParameterSchemaLimitationType.Integer | ParameterSchemaLimitationType.Number;

		public ParameterSchemaElement Parse(TemplateDictionaryAccessor lltSchema,
			ParameterSchemaLimitationType requestedType, ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			var isInteger = requestedType == ParameterSchemaLimitationType.Integer;

			if (ParameterSchemaParserHelpers.RequireNumber(lltSchema, "min", path, errors, out var min) is { } errMin)
				return errMin;
			if (ParameterSchemaParserHelpers.RequireNumber(lltSchema, "max", path, errors, out var max) is { } errMax)
				return errMax;

			double step = isInteger ? 1 : 0;
			if (ParameterSchemaParserHelpers.OptionalNumber(lltSchema, "step", path, errors, out var stepOpt) is { } errStep)
				return errStep;
			if (stepOpt.HasValue)
				step = stepOpt.Value;

			var def = min;
			if (ParameterSchemaParserHelpers.OptionalNumber(lltSchema, "default", path, errors, out var defOpt) is { } errDef)
				return errDef;
			if (defOpt.HasValue)
				def = defOpt.Value;

			if (isInteger)
			{
				min = Math.Round(min);
				max = Math.Round(max);
				step = Math.Round(step);
				def = Math.Round(def);
				if (step < 1)
					step = 1;
			}

			if (min > max)
				return ParameterSchemaParserHelpers.InvalidPropertyValue(path, errors,
					$"The 'min' value ({min}) must be less than or equal to the 'max' value ({max}).");

			if (defOpt.HasValue && (def < min || def > max))
				return ParameterSchemaParserHelpers.InvalidPropertyValue(path, errors,
					$"The 'default' value ({def}) must be within the range [{min}, {max}].");

			var element = new ParameterSliderElement
			{
				Min = min,
				Max = max,
				Step = step,
				Default = def,
				IsInteger = isInteger
			};
			return ParameterSchemaParserHelpers.ApplyCommonProperties(element, lltSchema, path, errors) ?? element;
		}
	}
}
