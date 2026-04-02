using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Data.Models
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
		/// Get or sets the settings profile ID that is currently used by this conversation.
		/// Used for accessing the <see cref=""/>
		/// </summary>
		public string SettingsProfile { get; set; } = ChatSettings.DefaultId;

		/// <summary>
		/// The ID of the root node in the conversation tree. Can be -1 if there is no elements in the conversation.
		/// </summary>
		public int RootNodeId { get; set; } = -1;

		/// <summary>
		/// The ID of the leaf node of the currently selected branch in the conversation tree. Can be -1 if there is no elements in the conversation.
		/// </summary>
		public int LeafNodeId { get; set; } = -1;

		/// <summary>
		/// The date and time when the conversation was created.
		/// </summary>
		public DateTime CreatedAt { get; set; } = DateTime.Now;

		/// <summary>
		/// The date and time when the conversation was last modified.
		/// </summary>
		public DateTime LastModifiedAt { get; set; } = DateTime.Now;
	}
}