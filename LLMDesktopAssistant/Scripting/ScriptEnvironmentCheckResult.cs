namespace LLMDesktopAssistant.Scripting
{
	public class ScriptEnvironmentCheckResult
	{
		/// <summary>
		/// Whether the environment check was successful or not.
		/// </summary>
		public required bool Success { get; init; }

		/// <summary>
		/// Optional message providing additional information about the result.
		/// </summary>
		public string? Message { get; init; }
	}
}
