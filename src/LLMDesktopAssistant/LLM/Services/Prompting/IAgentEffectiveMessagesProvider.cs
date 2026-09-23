using LLMDesktopAssistant.Agents;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// The source of truth for the effective chat context of an agent:
	/// applies the round window, agent visibility rules and active context checkpoints
	/// to the chat message sequence.
	/// </summary>
	public interface IAgentEffectiveMessagesProvider
	{
		/// <summary>
		/// Builds the effective chat context for the given agent.
		/// </summary>
		/// <param name="agent">The agent to build the context for.</param>
		EffectiveChatContext GetEffectiveMessages(ChatAgentDescriptor agent);
	}
}
