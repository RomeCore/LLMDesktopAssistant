using LiteDB;

namespace LLMDesktopAssistant.LLM.Conversations.Models
{
	/// <summary>
	/// Represents a root node in a conversation tree, containing messages and child nodes.
	/// </summary>
	public sealed class MessageRootNode
	{
		/// <summary>
		/// The unique identifier for the message node. This value is used to identify the message in the database.
		/// </summary>
		[BsonId]
		public int Id { get; set; }

		/// <summary>
		/// The index of the selected node in the conversation.
		/// </summary>
		public int SelectedNode { get; set; } = 0;

		/// <summary>
		/// The identifiers pointing to next nodes in the conversation.
		/// </summary>
		public int[] NextNodes { get; set; } = [];
	}
}
