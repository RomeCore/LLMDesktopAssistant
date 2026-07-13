using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.Prompting.Injectors;

// The massive AI-generated documentation warning!

/// <summary>
/// Defines a mechanism for injecting virtual <see cref="RawUserMessage"/> instances
/// into the message list used by <see cref="LLM.Services.PromptChatBuilder"/> when
/// building the LLM context for a given agent.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of <see cref="IPromptInjector"/> are executed <b>before</b>
/// message conversion and <b>before</b> <see cref="Hooks.IPromptBuildingHook"/> hooks.
/// They operate on a mutable <see cref="List{ChatMessage}"/> that contains both
/// original chat messages (<see cref="UserMessage"/>, <see cref="AssistantMessage"/>)
/// and previously injected <see cref="RawUserMessage"/> instances.
/// </para>
/// <para>
/// Injectors are ideal for scenarios where <b>entire synthetic messages</b> need to
/// appear in the LLM context — for example:
/// <list type="bullet">
///   <item>Reactions that were added to a message after it was sent.</item>
///   <item>User profile or description changes that occurred during the conversation.</item>
///   <item>System events (chat renamed, agent reconfigured, etc.).</item>
/// </list>
/// </para>
/// <para>
/// Injectors are ordered via <see cref="Order"/> (lower values execute first).
/// All injectors should be registered in the DI container as
/// <c>IEnumerable&lt;IPromptInjector&gt;</c>.
/// </para>
/// </remarks>
public interface IPromptInjector
{
	/// <summary>
	/// Gets the execution order. Lower values are executed first.
	/// Default is <c>0</c>.
	/// </summary>
	int Order => 0;

	/// <summary>
	/// Mutates the <paramref name="messages"/> list by inserting, removing, or
	/// reordering <see cref="RawUserMessage"/> instances as needed.
	/// </summary>
	/// <param name="messages">
	/// The current list of messages to be processed. Contains original
	/// <see cref="BranchedMessage"/> items from the chat history, plus any
	/// <see cref="RawUserMessage"/> items injected by earlier injectors.
	/// </param>
	/// <param name="agent">
	/// The agent that will receive this context.
	/// Can be used to filter or customize injected content per agent.
	/// </param>
	void Inject(List<BranchedMessage> messages, ChatAgentDescriptor agent);
}
