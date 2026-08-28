namespace LLMDesktopAssistant.Prompting.Parameterization
{
	public class ParameterSchemaParsingError
	{
		public required ParameterSchemaPath Path { get; init; }

		public required ParameterSchemaParsingErrorType Type { get; init; }

		public required string? Message { get; init; }
	}
}
