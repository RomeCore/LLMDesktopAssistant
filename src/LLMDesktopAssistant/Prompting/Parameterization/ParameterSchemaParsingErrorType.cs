namespace LLMDesktopAssistant.Prompting.Parameterization
{
	public enum ParameterSchemaParsingErrorType
	{
		Other,

		SchemaIsNotDictionary,

		MissingType,

		UnknownType,

		UnsupportedValueType,

		MissingProperty,

		InvalidPropertyType,

		InvalidPropertyValue,
	}
}
