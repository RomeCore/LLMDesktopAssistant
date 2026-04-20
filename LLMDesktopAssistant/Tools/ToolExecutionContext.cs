using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// The context in which a tool is executed.
	/// This used to provide additional information about the execution environment of a tool.
	/// </summary>
	public class ToolExecutionContext
	{
		public required Chat Chat { get; init; }
	}
}