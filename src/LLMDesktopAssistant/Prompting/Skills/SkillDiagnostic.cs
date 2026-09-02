namespace LLMDesktopAssistant.Prompting.Skills
{
	public class SkillDiagnostic
	{
		public required bool IsFatal { get; init; }

		public required SkillDiagnosticCode Codes { get; init; }

		public required Exception? Exception { get; init; }
	}
}
