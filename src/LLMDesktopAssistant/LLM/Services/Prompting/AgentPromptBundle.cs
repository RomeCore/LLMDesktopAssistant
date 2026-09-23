using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// The composed prompt bundle: the full message sequence and the tool set for an LLM request.
	/// </summary>
	public record AgentPromptBundle(IReadOnlyList<IMessage> Messages, IReadOnlyList<FunctionTool> Tools);
}
