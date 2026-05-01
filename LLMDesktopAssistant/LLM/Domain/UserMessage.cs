using LLMDesktopAssistant.Utils;
using System.Collections.Immutable;

namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents a user message in the chat session.
	/// </summary>
	public class UserMessage : ChatMessage
	{
		/// <summary>
		/// The collection of attachments associated with the user message. These can include images or files.
		/// </summary>
		public required ImmutableList<Attachment> Attachments { get; init; }

		/// <summary>
		/// The login name of the sender of the message. This is used to identify the user within the system.
		/// </summary>
		public required string SenderLogin { get; init; }

		/// <summary>
		/// The visibility settings for the user message. Determines who can see the message.
		/// </summary>
		public required MessageVisibility Visibility { get; init; }

		/// <summary>
		/// The collection of users (logins) or agents (guids) to whom the message is visible.
		/// If empty, it means that the message is visible to all users and agents.
		/// </summary>
		public required ImmutableList<string> VisibleTo { get; init; }

		/// <summary>
		/// Indicates whether the message visibility is controlled by a whitelist or blacklist. If true, only users in the VisibleTo list can see the message. If false, all users except those in the VisibleTo list can see the message.
		/// </summary>
		public required bool IsVisibleToWhiteList { get; init; }
	}
}