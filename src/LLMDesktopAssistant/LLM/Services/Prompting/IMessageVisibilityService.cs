using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// Determines whether chat messages are visible to a given agent, respecting the agent's
	/// read permissions, the sender agent's exposure mode and the message visibility settings.
	/// </summary>
	public interface IMessageVisibilityService
	{
		/// <summary>
		/// Determines whether the specified user message is visible to the given agent.
		/// </summary>
		/// <param name="message">The branched user message to check.</param>
		/// <param name="agent">The agent to check visibility for.</param>
		/// <returns><see langword="true"/> if the message is visible to the agent; otherwise, <see langword="false"/>.</returns>
		bool IsUserMessageVisibleToAgent(BranchedMessage message, ChatAgentDescriptor agent);

		/// <summary>
		/// Determines whether the specified assistant message is visible to the given agent.
		/// </summary>
		/// <param name="message">The branched assistant message to check.</param>
		/// <param name="agent">The agent to check visibility for.</param>
		/// <returns><see langword="true"/> if the message is visible to the agent; otherwise, <see langword="false"/>.</returns>
		bool IsAssistantMessageVisibleToAgent(BranchedMessage message, ChatAgentDescriptor agent);
	}
}
