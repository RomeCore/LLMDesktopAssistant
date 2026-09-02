using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.StructuredValues.Parameterization.Parsers;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.StructuredValues.Parameterization
{
	[Service(typeof(IParameterSchemaParserManager))]
	public class ParameterSchemaParserManager(
		IServiceProvider services
	) : IParameterSchemaParserManager
	{
		private Dictionary<string, IParameterSchemaParser> _parsers = null!;

		private void EnsureParsersInitialized()
		{
			_parsers ??= services.GetServices<IParameterSchemaParser>()
				.ToDictionary(p => p.ElementType);
		}

		public ParameterSchemaElement Parse(INodeValue schema,
			ParameterSchemaPath path, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			ArgumentNullException.ThrowIfNull(schema);
			EnsureParsersInitialized();

			if (schema is not INodeDictionaryValue dictSchema)
			{
				return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
				{
					Type = ParameterSchemaParsingErrorType.SchemaIsNotDictionary,
					Path = path,
					Message = "Schema is not a dictionary."
				}, errors);
			}

			if (!dictSchema.Items.TryGetValue("type", out var typeAccessor) || typeAccessor is not INodeStringValue typeStringValue)
			{
				return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
				{
					Type = ParameterSchemaParsingErrorType.MissingType,
					Path = path.Append("type"),
					Message = $"Expected a 'type' field of type 'string', but found '{(typeAccessor is null ? "null" : ParameterSchemaParserHelpers.GetNodeTypeName(typeAccessor))}'."
				}, errors);
			}

			var splitType = typeStringValue.Value!.Split(['/'], 2);
			var elementType = splitType[0];
			var valueTypeStr = splitType.Length == 2 ? splitType[1] : null;
			var valueType = valueTypeStr switch
			{
				"boolean" => ParameterSchemaLimitationType.Boolean,
				"integer" => ParameterSchemaLimitationType.Integer,
				"number" => ParameterSchemaLimitationType.Number,
				"string" => ParameterSchemaLimitationType.String,
				"object" => ParameterSchemaLimitationType.Object,
				null => ParameterSchemaLimitationType.NotSpecified,
				_ => (ParameterSchemaLimitationType?)null
			};

			var elementUnknown = !_parsers.TryGetValue(elementType, out var parser);
			var valueUnknown = valueType is null;
			if (elementUnknown || valueUnknown)
			{
				string message;
				if (elementUnknown && valueUnknown)
					message = $"Unknown element type '{elementType}' and unknown value type '{valueTypeStr}'.";
				else if (elementUnknown)
					message = $"Unknown element type '{elementType}'.";
				else
					message = $"Unknown value type '{valueTypeStr}'.";

				return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
				{
					Type = ParameterSchemaParsingErrorType.UnknownType,
					Path = path.Append("type"),
					Message = message
				}, errors);
			}

			var supportedValueTypes = parser!.SupportedValueTypes;
			if (!supportedValueTypes.HasFlag(valueType!))
			{
				return new ParameterSchemaErrorElement(new ParameterSchemaParsingError
				{
					Type = ParameterSchemaParsingErrorType.UnsupportedValueType,
					Path = path.Append("type"),
					Message = $"Unsupported value type '{valueType}' for element type '{elementType}'."
				}, errors);
			}

			return parser.Parse(dictSchema, valueType!.Value, path, errors);
		}

		public ParameterSchema ParseRoot(INodeValue schema, AppendOnlyList<ParameterSchemaParsingError> errors)
		{
			if (schema is not INodeDictionaryValue dictSchema)
				throw new ArgumentException("Schema must be a dictionary.", nameof(schema));

			var objectSchema = new ParameterSchemaObjectElement();
			var path = ParameterSchemaPath.Root;

			foreach (var (key, value) in dictSchema.Items)
			{
				objectSchema.Properties.Add(key, Parse(value, path.Append(key), errors));
			}

			return new ParameterSchema
			{
				Root = objectSchema
			};
		}
	}
}
