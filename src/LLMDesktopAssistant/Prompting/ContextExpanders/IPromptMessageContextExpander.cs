using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.Prompting.ContextExpanders
{
	/// <summary>
	/// Interface for expanding the message prompt context for rendering prompt templates.
	/// Used for <see cref="BranchedMessage"/>.
	/// </summary>
	public interface IPromptMessageContextExpander
	{
		/// <summary>
		/// Expands the prompt context with additional information.
		/// </summary>
		/// <param name="message">The chat message that is rendeing now.</param>
		/// <param name="agent">The agent that the prompt is rendered for. Can be null if no specific agent is associated with the prompt.</param>
		/// <param name="context">The current prompt context as a dictionary.</param>
		void ExpandPromptContext(BranchedMessage message, ChatAgentDescriptor? agent, Dictionary<string, object?> context);
	}
}