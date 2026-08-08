using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// The context passed to <see cref="IChatExecutionHook"/> methods.
	/// </summary>
	public sealed class ChatPreExecutionHookContext
	{
		/// <summary>
		/// Gets the chat instance where the execution is happening.
		/// </summary>
		public required Chat Chat { get; init; }

		/// <summary>
		/// Gets the agent descriptor that generated the response.
		/// </summary>
		public required ChatAgentDescriptor Agent { get; init; }

		/// <summary>
		/// Gets the assistant message that is will be generated in the next cycle.
		/// </summary>
		public required AssistantMessage Response { get; init; }

		/// <summary>
		/// Gets the zero-based index of the completed cycle within the agent response chain.
		/// </summary>
		public int Cycle { get; init; }
	}
}
