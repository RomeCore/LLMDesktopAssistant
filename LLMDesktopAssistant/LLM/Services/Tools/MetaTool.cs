using System.Text.Json.Nodes;
using LLMDesktopAssistant.Scripting;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	public class MetaTool
	{
		public required string Name { get; init; }

		public required string Title { get; init; }

		public required string Description { get; init; }

		public required string Category { get; init; }

		public required bool AskForConfirmation { get; init; }

		public required JsonObject? ArgumentSchema { get; init; }

		public required ScriptLanguageType ScriptLanguage { get; init; }

		public required string ExecutionCode { get; init; }
	}
}