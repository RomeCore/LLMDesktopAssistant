namespace LLMDesktopAssistant.LLM.Settings
{
	public class MemorySettings : NotifyPropertyChanged
	{
		private bool _enableMemory;
		public bool EnableMemory
		{
			get => _enableMemory;
			set => SetProperty(ref _enableMemory, value);
		}

		private bool _automaticRetrievalEnabled;
		/// <summary>
		/// Gets or sets a value indicating whether automatic retrieval of memory is enabled.
		/// </summary>
		public bool AutomaticRetrievalEnabled
		{
			get => _automaticRetrievalEnabled;
			set => SetProperty(ref _automaticRetrievalEnabled, value);
		}

		private string _retrievalModel = string.Empty;
		/// <summary>
		/// The model used for automatic memory retrieval.
		/// </summary>
		public string RetrievalModel
		{
			get => _retrievalModel;
			set => SetProperty(ref _retrievalModel, value);
		}

		private bool _automaticRecordingEnabled;
		/// <summary>
		/// Whether automatic memory recording for the chat is enabled.
		/// </summary>
		public bool AutomaticRecordingEnabled
		{
			get => _automaticRecordingEnabled;
			set => SetProperty(ref _automaticRecordingEnabled, value);
		}

		private string _recordingModel = string.Empty;
		/// <summary>
		/// The model used for automatic memory recording.
		/// </summary>
		public string RecordingModel
		{
			get => _recordingModel;
			set => SetProperty(ref _recordingModel, value);
		}
	}
}
