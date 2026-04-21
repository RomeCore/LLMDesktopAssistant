using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

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
		/// Gets or sets the message ID that this attachment belongs to.
		/// </summary>
		public int MessageId { get; set; }

		/// <summary>
		/// Gets or sets the title of the attachment.
		/// This is used for display purposes in UI components.
		/// </summary>
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the source URL of the attachment.
		/// This can be a web URL, a local path or a reference to the MCP resource (example: mcp:server_name://some/resource/name.txt).
		/// </summary>
		public string SourceUrl { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the local path relative to the working folder (see <c>Chat.Settings.WorkingFolder</c>).
		/// This is where attachment file is copied and can be used for tools like Python, Filesystem, Shell interpreters, etc.
		/// </summary>
		public string LocalPath { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the size of the attachment in bytes.
		/// </summary>
		public long Size { get; set; } = 0;

		/// <summary>
		/// Gets or sets any additional information about the attachment to be sent to the LLM.
		/// </summary>
		public string? AdditionalInfo { get; set; }

		/// <summary>
		/// Gets or sets the preview content of the attachment.
		/// This will be shown to the LLM if available.
		/// May contain entire file contents, first few lines, hex binary representation, image or sound description, etc.
		/// </summary>
		public string? PreviewContent { get; set; } = null;
	}
}