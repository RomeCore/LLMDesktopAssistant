namespace LLMDesktopAssistant.LLM.Domain
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

		private string _sourceUrl = string.Empty;
		public string SourceUrl
		{
			get => _sourceUrl;
			set => SetProperty(ref _sourceUrl, value);
		}

		private int _startLineIndex = 0;
		public int StartLineIndex
		{
			get => _startLineIndex;
			set => SetProperty(ref _startLineIndex, value);
		}

		private int _lineCount = 10;
		public int LineCount
		{
			get => _lineCount;
			set => SetProperty(ref _lineCount, value);
		}

		private int _startByteIndex = 0;
		public int StartByteIndex
		{
			get => _startByteIndex;
			set => SetProperty(ref _startByteIndex, value);
		}

		private int _byteCount = 100;
		public int ByteCount
		{
			get => _byteCount;
			set => SetProperty(ref _byteCount, value);
		}
	}
}