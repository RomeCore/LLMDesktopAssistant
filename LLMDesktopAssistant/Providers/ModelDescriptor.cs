using RCLargeLanguageModels;

namespace LLMDesktopAssistant.Providers
{
	public class ModelDescriptor : NotifyPropertyChanged
	{
		private string _name = string.Empty;
		/// <summary>
		/// Gets or sets the name of the model.
		/// </summary>
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private bool _isInformationKnown = false;
		/// <summary>
		/// Gets or sets a value indicating whether the information about the model is known
		/// (e.g. <see cref="InputModalities"/>, <see cref="Capabilities"/>, <see cref="MaxOutputTokens"/>, etc.).
		/// If unknown, the <see cref="ModelDescriptorsCache"/>'s <see cref="ModelDescriptor"/> will be used for info.
		/// </summary>
		public bool IsInformationKnown
		{
			get => _isInformationKnown;
			set => SetProperty(ref _isInformationKnown, value);
		}

		private string _displayName = string.Empty;
		/// <summary>
		/// Gets or sets the display human-readable name of the model.
		/// </summary>
		public string DisplayName
		{
			get => _displayName;
			set => SetProperty(ref _displayName, value);
		}

		private LLMModalities _inputModalities = LLMModalities.Text;
		public LLMModalities InputModalities
		{
			get => _inputModalities;
			set => SetProperty(ref _inputModalities, value);
		}

		private LLMModalities _outputModalities = LLMModalities.Text;
		public LLMModalities OutputModalities
		{
			get => _outputModalities;
			set => SetProperty(ref _outputModalities, value);
		}

		private LLMCapabilities _capabilities = LLMCapabilities.Unknown;
		public LLMCapabilities Capabilities
		{
			get => _capabilities;
			set => SetProperty(ref _capabilities, value);
		}

		private int _contextSize = -1;
		public int ContextSize
		{
			get => _contextSize;
			set => SetProperty(ref _contextSize, value);
		}

		private int _maxOutputTokens = -1;
		public int MaxOutputTokens
		{
			get => _maxOutputTokens;
			set => SetProperty(ref _maxOutputTokens, value);
		}

		private decimal _inputTokenCost = 0.0m;
		public decimal InputTokenCost
		{
			get => _inputTokenCost;
			set => SetProperty(ref _inputTokenCost, value);
		}

		private decimal _inputCacheTokenCost = 0.0m;
		public decimal InputCacheTokenCost
		{
			get => _inputCacheTokenCost;
			set => SetProperty(ref _inputCacheTokenCost, value);
		}

		private decimal _outputTokenCost = 0.0m;
		public decimal OutputTokenCost
		{
			get => _outputTokenCost;
			set => SetProperty(ref _outputTokenCost, value);
		}
	}
}
