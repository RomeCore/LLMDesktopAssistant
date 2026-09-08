using RCLargeLanguageModels.Messages.Attachments;

namespace LLMDesktopAssistant.LLM.MVVM.Additional
{
	/// <summary>
	/// Represents a file (or URL) attached to a message, tool call result or the input state.
	/// The file can be copied to the working directory so tools can access it by <see cref="LocalPath"/>,
	/// and can provide a native attachment (<see cref="NativeAttachment"/>) that is sent to the LLM API.
	/// </summary>
	public class AttachmentMessagePart : AdditionalMessagePart
	{
		private string? _title;
		/// <summary>
		/// Gets or sets the display title of the attachment (usually the file name).
		/// </summary>
		public string? Title
		{
			get => _title;
			set => SetProperty(ref _title, value);
		}

		private string? _sourceUrl;
		/// <summary>
		/// Gets or sets the source URL of the attachment.
		/// This can be a web URL or a local absolute path.
		/// </summary>
		public string? SourceUrl
		{
			get => _sourceUrl;
			set => SetProperty(ref _sourceUrl, value);
		}

		private string? _localPath;
		/// <summary>
		/// Gets or sets the local path relative to the working folder.
		/// This is where the attachment file is copied and can be used by tools like Python,
		/// Filesystem, Shell interpreters, etc.
		/// </summary>
		public string? LocalPath
		{
			get => _localPath;
			set => SetProperty(ref _localPath, value);
		}

		private long _size;
		/// <summary>
		/// Gets or sets the size of the attachment in bytes.
		/// </summary>
		public long Size
		{
			get => _size;
			set => SetProperty(ref _size, value);
		}

		private IAttachment? _nativeAttachment;
		/// <summary>
		/// Gets or sets the native attachment that can be sent directly to a large language model.
		/// </summary>
		public IAttachment? NativeAttachment
		{
			get => _nativeAttachment;
			set => SetProperty(ref _nativeAttachment, value);
		}
	}
}
