using System.Collections.Immutable;

namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents the user's input used for generating a response from the LLM assistant.
	/// </summary>
	public class UserInput
	{
		/// <summary>
		/// Gets or sets the content of the user input. This is typically the text entered by the user in a chat interface.
		/// </summary>
		public required string Content { get; init; }

		/// <summary>
		/// Gets or sets the collection of attachments associated with the user input. These can include images, files, or other types of data. If no attachments are present, this property is an empty collection.
		/// </summary>
		public ImmutableList<Attachment> Attachments { get; init; } = [];

		/// <summary>
		/// Gets or sets the visibility of the user input. Determines who can see the message.
		/// </summary>
		public MessageVisibility Visibility { get; init; } = MessageVisibility.Always;

		/// <summary>
		/// Gets or sets the list of users (logins) or agents (guids) to whom the message is visible.
		/// If empty, it means that the message is visible to all.
		/// </summary>
		public ImmutableList<string> VisibleTo { get; init; } = [];
	}
}