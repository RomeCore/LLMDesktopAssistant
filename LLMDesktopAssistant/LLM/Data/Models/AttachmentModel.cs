using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace LLMDesktopAssistant.LLM.Data.Models
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
		/// Gets or sets the message ID that this attachment belongs to.
		/// </summary>
		public int MessageId { get; set; }

		/// <summary>
		/// Gets or sets the display name of the attachment.
		/// </summary>
		public string DisplayName { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the type of the attachment.
		/// </summary>
		public AttachmentTypeModel Type { get; set; }
	}
}