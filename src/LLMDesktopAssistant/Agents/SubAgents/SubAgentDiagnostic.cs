namespace LLMDesktopAssistant.Agents.SubAgents
{
	public class SubAgentDiagnostic
	{
		public required bool IsFatal { get; init; }

		public required SubAgentDiagnosticCode Codes { get; init; }

		public required Exception? Exception { get; init; }
	}
}
