using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace LLMDesktopAssistant.LLM.Conversations.Models
{
	/// <summary>
	/// Represents a node in a conversation tree, containing messages and child nodes.
	/// </summary>
	public sealed class MessageNode
	{
		/// <summary>
		/// The unique identifier for the message node. This value is used to identify the message in the database.
		/// </summary>
		[BsonId]
		public int Id { get; set; }

		/// <summary>
		/// The identifier that leads to target <see cref="Message"/> associated with this node.
		/// </summary>
		public int MessageId {  get; set; }

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
