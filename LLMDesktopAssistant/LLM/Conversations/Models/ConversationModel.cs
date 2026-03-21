using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace LLMDesktopAssistant.LLM.Conversations.Models
{
	/// <summary>
	/// Represents a conversation model in the database.
	/// </summary>
	public sealed class ConversationModel
	{
		/// <summary>
		/// The unique identifier for the сonversation.
		/// </summary>
		[BsonId]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the title that should be displayed to the user.
		/// </summary>
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the base system instructions for this conversation.
		/// </summary>
		public string SystemInstructions { get; set; } = string.Empty;

		/// <summary>
		/// The ID of the root node in the conversation tree. Can be -1 if there is no elements in the conversation.
		/// </summary>
		public int RootNodeId { get; set; } = -1;

		/// <summary>
		/// The ID of the leaf node of the currently selected branch in the conversation tree. Can be -1 if there is no elements in the conversation.
		/// </summary>
		public int LeafNodeId { get; set; } = -1;
	}
}