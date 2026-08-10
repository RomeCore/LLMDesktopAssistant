using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using RCLargeLanguageModels.Metadata;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// The context passed to <see cref="IChatExecutionHook"/> methods.
	/// </summary>
	public sealed class ChatAgentExecutionHookContext
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
		/// Gets the assistant messages that was generated in the completed cycle.
		/// </summary>
		public required ImmutableList<AssistantMessage> Responses { get; init; }

		/// <summary>
		/// Gets a value indicating whether the completed cycle contained tool calls.
		/// </summary>
		public bool HasToolCalls { get; init; }

		/// <summary>
		/// Gets the zero-based index of the completed cycle within the agent response chain.
		/// </summary>
		public int Cycle { get; init; }
	}
}
