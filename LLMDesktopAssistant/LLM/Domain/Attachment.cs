using System.Text.Json.Serialization;
using LiteDB;
using LLMDesktopAssistant.Utils.Files;
using RCLargeLanguageModels.Messages.Attachments;

namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents an attachment to the message or tool call result.
	/// </summary>
	public class Attachment
	{
		/// <summary>
		/// Gets or sets the GUID for this attachment, used for persistence, especially for removing it from the database.
		/// Do not change this GUID by itself.
		/// </summary>
		public Guid Guid { get; set; } = Guid.NewGuid();

		/// <summary>
		/// Gets or sets the title of the attachment.
		/// This is used for display purposes in UI components.
		/// </summary>
		public required string Title { get; init; }

		/// <summary>
		/// Gets or sets the source URL of the attachment.
		/// This can be a web URL, a local path or a reference to the MCP resource (example: mcp://server_name/some/resource/name.txt).
		/// </summary>
		public required string SourceUrl { get; init; }

		/// <summary>
		/// Gets or sets the local path relative to the working folder (see <c>Chat.Settings.WorkingFolder</c>).
		/// This is where attachment file is copied and can be used for tools like Python, Filesystem, Shell interpreters, etc.
		/// </summary>
		public required string LocalPath { get; init; }

		/// <summary>
		/// Gets or sets the size of the attachment in bytes.
		/// </summary>
		public required long Size { get; init; }

		/// <summary>
		/// Gets the display size of the attachment in a human-readable format.
		/// Example: "50 KB", "35 MB", "12 GB".
		/// </summary>
		[BsonIgnore]
		[JsonIgnore]
		public string DisplaySize => FileUtils.BytesToDisplaySize(Size);

		/// <summary>
		/// Gets or sets the number of lines in the attachment if it is a text file, otherwise null.
		/// </summary>
		public required int? Lines { get; init; }

		/// <summary>
		/// Gets whether this attachment is binary (i.e., not a text file).
		/// </summary>
		public bool IsBinary => Lines == null;

		/// <summary>
		/// Gets or sets the native attachment that can be sent directly to a large language model.
		/// </summary>
		public IAttachment? NativeAttachment { get; init; } = null;
	}
}