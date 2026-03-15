using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace LLMDesktopAssistant.LLM.Conversations.Models
{
	public sealed class Conversation
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
		/// Gets or sets the ID that points to the <see cref="MessageRootNode"/>.
		/// </summary>
		public int RootNodeId { get; set; }
	}
}