using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// The context in which a tool is executed.
	/// This used to provide additional information about the execution environment of a tool.
	/// </summary>
	public class ToolExecutionContext
	{
		/// <summary>
		/// The chat instance where tool is being executed.
		/// </summary>
		public required Chat Chat { get; init; }

		/// <summary>
		/// The assistant message that contains tool call that being executed.
		/// </summary>
		public required AssistantMessage Message { get; init; }

		/// <summary>
		/// The tool call that is being executed.
		/// </summary>
		public required ToolCall Call { get; init; }

		/// <summary>
		/// Information about the tool that is being executed.
		/// </summary>
		public required ToolInfo Info { get; init; }
	}
}