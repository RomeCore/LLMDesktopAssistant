namespace LLMDesktopAssistant.Core.LLM.Domain
{
	/// <summary>
	/// Represents an attachment to the user message.
	/// </summary>
	public class Attachment
	{
		/// <summary>
		/// Gets or sets the title of the attachment.
		/// This is used for display purposes in UI components.
		/// </summary>
		public required string Title { get; init; }

		/// <summary>
		/// Gets or sets the source URL of the attachment.
		/// This can be a web URL, a local path or a reference to the MCP resource (example: mcp:server_name://some/resource/name.txt).
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
		public string DisplaySize
		{
			get
			{
				var size = Size;
				var result = $"{size} B";

				if (size > 10240)
				{
					size /= 1024;
					result = $"{size} KB";

					if (size > 10240)
					{
						size /= 1024;
						result = $"{size} MB";

						if (size > 10240)
						{
							size /= 1024;
							result = $"{size} GB";
						}
					}
				}

				return result;
			}
		}

		/// <summary>
		/// Gets or sets any additional information about the attachment to be sent to the LLM.
		/// </summary>
		public string? AdditionalInfo { get; init; } = null;

		/// <summary>
		/// Gets or sets the preview content of the attachment.
		/// This will be shown to the LLM if available.
		/// May contain entire file contents, first few lines, hex binary representation, image or sound description, etc.
		/// </summary>
		public string? PreviewContent { get; init; } = null;
	}
}