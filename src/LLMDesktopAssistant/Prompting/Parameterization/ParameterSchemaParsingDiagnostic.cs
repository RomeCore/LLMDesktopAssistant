namespace LLMDesktopAssistant.Prompting.Parameterization
{
	public class ParameterSchemaParsingDiagnostic
	{
		public required bool IsFatal { get; init; }

		public required ImmutableList<ParameterSchemaParsingError> Errors { get; init; }

		public required Exception? Exception { get; init; }
	}
}
