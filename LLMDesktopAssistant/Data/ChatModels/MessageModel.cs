using LiteDB;
using LLMDesktopAssistant.LLM.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Data.ChatModels
{
	/// <summary>
	/// Represents a message in a conversation inside a database.
	/// </summary>
	public class MessageModel
	{
		/// <summary>
		/// The unique identifier for the message.
		/// </summary>
		[BsonId]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the time that this message is created at.
		/// </summary>
		public DateTime CreatedAt { get; set; } = DateTime.Now;

		/// <summary>
		/// Gets or sets the role of the message that describes its type.
		/// </summary>
		public RoleModel Role { get; set; }

		/// <summary>
		/// Gets or sets the status of the message.
		/// </summary>
		public MessageStatusModel Status { get; set; }

		/// <summary>
		/// Gets or sets the sender of the message. This can be GUID for assistant message and login name for user message.
		/// </summary>
		public string Sender { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the visibility of the message. This can be used to control whether the message is visible to all users/agents or aonly specific.
		/// </summary>
		public MessageVisibility Visibility { get; set; } = MessageVisibility.Always;

		/// <summary>
		/// Gets or sets a list of users/agents that this message is visible to.
		/// </summary>
		public ImmutableList<string> VisibleTo { get; set; } = [];

		/// <summary>
		/// Gets or sets the main content of the message.
		/// </summary>
		public string Content { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the reasoning content of the assistant message.
		/// </summary>
		public string? ReasoningContent { get; set; }

		/// <summary>
		/// Gets or sets the error associated with the message, if any.
		/// </summary>
		public string? Error { get; set; }

		/// <summary>
		/// Gets or sets the summary of this message and previous messages until another summary (and including it).
		/// </summary>
		public string? SummaryOfPrevMessages { get; set; }
	}
}