using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.Data.ChatModels
{
	/// <summary>
	/// The attachment that can be applied to message. Used mostly for display purposes.
	/// </summary>
	public class AttachmentModel
	{
		/// <summary>
		/// The unique identifier for the attachment.
		/// </summary>
		[BsonId]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this attachment is associated with a tool call.
		/// </summary>
		public bool IsParentToolCall { get; set; }

		/// <summary>
		/// Gets or sets the message or tool call ID that this attachment belongs to.
		/// </summary>
		public int ParentId { get; set; }

		/// <summary>
		/// Gets or sets the attachment data.
		/// </summary>
		public Attachment Attachment { get; set; } = null!;
	}
}