namespace LLMDesktopAssistant.Core.LLM.Domain
{
	/// <summary>
	/// Represents parameters for how attachments are applied in the user message.
	/// </summary>
	public class AttachmentApplicationParameters :  NotifyPropertyChanged
	{
		private AttachmentApplicationMode _mode = AttachmentApplicationMode.OnlyReference;
		public AttachmentApplicationMode Mode
		{
			get => _mode;
			set => SetProperty(ref _mode, value);
		}

		private Uri _sourceUri = new Uri("https://example.com");
		public Uri SourceUri
		{
			get => _sourceUri;
			set => SetProperty(ref _sourceUri, value);
		}

		private int _startLine = 1;
		public int StartLine
		{
			get => _startLine;
			set => SetProperty(ref _startLine, value);
		}

		private int _endLine = 100;
		public int EndLine
		{
			get => _endLine;
			set => SetProperty(ref _endLine, value);
		}

		private int _startByte = 1;
		public int StartByte
		{
			get => _startByte;
			set => SetProperty(ref _startByte, value);
		}

		private int _endByte = 1024;
		public int EndByte
		{
			get => _endByte;
			set => SetProperty(ref _endByte, value);
		}
	}
}