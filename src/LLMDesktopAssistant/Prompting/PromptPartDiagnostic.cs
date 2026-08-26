namespace LLMDesktopAssistant.Prompting
{
	public class PromptPartDiagnostic
	{
		public required bool IsFatal { get; init; }

		public required PromptPartDiagnosticCode Code { get; init; }

		public ImmutableList<string> Messages { get; init; } = [];

		public Exception? Exception { get; init; } = null;
	}
}