using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels.Clients;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes an agent's generation settings.
	/// </summary>
	public class AgentGenerationSettings : NotifyPropertyChanged
	{
		private bool _enableCustomModel = false;
		/// <summary>
		/// Whether to enable custom model settings overriding the default model.
		/// </summary>
		public bool EnableCustomModel
		{
			get => _enableCustomModel;
			set => SetProperty(ref _enableCustomModel, value);
		}

		private LLModelDescriptorTracked _model = LLModelDescriptorTracked.Empty;
		/// <summary>
		/// The model to use for the agent.
		/// </summary>
		public LLModelDescriptorTracked Model
		{
			get => _model;
			set => SetProperty(ref _model, value);
		}

		private bool _enableReasoningSettings = false;
		/// <summary>
		/// Whether to enable reasoning settings overriding the model's default settings.
		/// </summary>
		public bool EnableReasoningSettings
		{
			get => _enableReasoningSettings;
			set => SetProperty(ref _enableReasoningSettings, value);
		}

		private ReasoningSettings _reasoningSettings = ReasoningSettings.Disabled;
		/// <summary>
		/// The reasoning settings to use for the agent. These override the model's default settings if <see cref="EnableReasoningSettings"/> is true.
		/// </summary>
		public ReasoningSettings ReasoningSettings
		{
			get => _reasoningSettings;
			set => SetProperty(ref _reasoningSettings, value);
		}

		private bool _enableTemperature = false;
		/// <summary>
		/// Whether to enable temperature settings overriding the model's default settings.
		/// </summary>
		public bool EnableTemperature
		{
			get => _enableTemperature;
			set => SetProperty(ref _enableTemperature, value);
		}

		private float _temperature = 1.0f;
		/// <summary>
		/// The temperature to use for the agent in range from 0 to 2. This overrides the model's default settings if <see cref="EnableTemperature"/> is true.
		/// </summary>
		public float Temperature
		{
			get => _temperature;
			set => SetProperty(ref _temperature, value);
		}

		private bool _enableMaxTokens = false;
		/// <summary>
		/// Whether to enable max tokens settings overriding the model's default settings.
		/// </summary>
		public bool EnableMaxTokens
		{
			get => _enableMaxTokens;
			set => SetProperty(ref _enableMaxTokens, value);
		}

		private int _maxTokens = 8096;
		/// <summary>
		/// The maximum number of tokens to generate for the agent. This overrides the model's default settings if <see cref="EnableMaxTokens"/> is true.
		/// </summary>
		public int MaxTokens
		{
			get => _maxTokens;
			set => SetProperty(ref _maxTokens, value);
		}

		private RangeObservableCollection<AdditionalParameter> _additionalParameters = [];
		/// <summary>
		/// The additional parameters to use for the agent. These are represented in a key-value format and passed to API.
		/// </summary>
		public RangeObservableCollection<AdditionalParameter> AdditionalParameters
		{
			get => _additionalParameters;
			set => _additionalParameters.Reset(value);
		}
	}
}