namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents parameters for how attachments are applied in the user message.
	/// </summary>
	public class AttachmentApplicationParameters :  NotifyPropertyChanged
	{
		private Uri _sourceUri = null!;
		/// <summary>
		/// Gets or sets the URI of the source attachment.
		/// </summary>
		public Uri SourceUri
		{
			get => _sourceUri;
			set => SetProperty(ref _sourceUri, value);
		}

		private bool _copyToWorkingDirectory = true;
		/// <summary>
		/// Indicates whether the attachment should be copied to the working directory.
		/// </summary>
		public bool CopyToWorkingDirectory
		{
			get => _copyToWorkingDirectory;
			set => SetProperty(ref _copyToWorkingDirectory, value);
		}

		private bool _applyNative = false;
		/// <summary>
		/// Determines whether to apply the native LLM attachment too.
		/// </summary>
		public bool ApplyNative
		{
			get => _applyNative;
			set => SetProperty(ref _applyNative, value);
		}
	}
}