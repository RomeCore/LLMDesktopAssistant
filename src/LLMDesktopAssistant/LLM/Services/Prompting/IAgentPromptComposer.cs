using LLMDesktopAssistant.Agents;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// The central prompt composer: builds the atomic pair (messages + tools) for an agent request.
	/// Owns the toolset cache invalidation and the system prompt header selection.
	/// </summary>
	public interface IAgentPromptComposer
	{
		/// <summary>
		/// Builds the prompt bundle for the given agent.
		/// </summary>
		/// <param name="agent">The agent to build the prompt for.</param>
		AgentPromptBundle Build(ChatAgentDescriptor agent);
	}
}
