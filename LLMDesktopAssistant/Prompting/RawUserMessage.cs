using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.Prompting;

/// <summary>
/// Represents a virtual user message that exists only within the prompt building context.
/// Unlike a regular <see cref="UserMessage"/>, a <see cref="RawUserMessage"/> is:
/// <list type="bullet">
///   <item>Not stored in <see cref="Chat.Messages"/> or the database.</item>
///   <item>Not associated with any sender, attachments, or visibility rules.</item>
///   <item>Used solely by <see cref="Injectors.IPromptInjector"/> implementations to inject
///       contextual information (e.g., reactions, user profile changes, system events)
///       into the LLM context without polluting the permanent chat history.</item>
/// </list>
/// </summary>
/// <remarks>
/// <see cref="RawUserMessage"/> is treated as a regular <see cref="ChatMessage"/> during
/// the conversion phase in <see cref="LLM.Services.PromptChatBuilder"/>, but it is
/// filtered out before any persistence or UI rendering occurs.
/// </remarks>
public sealed class RawUserMessage : ChatMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RawUserMessage"/> class
	/// with the specified textual content.
	/// </summary>
	/// <param name="content">The text content of this injected message.</param>
	public RawUserMessage(string content)
	{
		Content = content;
	}

	/// <summary>
	/// Gets or sets an optional category label for this raw message.
	/// Categories can be used for sorting, filtering, or debugging purposes
	/// (e.g., "reactions", "sender_info", "system_event").
	/// </summary>
	public string? Category { get; init; }
}
