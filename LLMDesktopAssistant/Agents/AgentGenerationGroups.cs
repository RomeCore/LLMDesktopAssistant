namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Represents the custom model group of the agent generation settings: the enable flag
	/// and the model override used instead of the chat's default model.
	/// </summary>
	public class CustomModelSettings : NotifyPropertyChanged
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

		private string _model = string.Empty;
		/// <summary>
		/// The model to use for the agent. Format: "ProviderName$ModelName".
		/// </summary>
		public string Model
		{
			get => _model;
			set => SetProperty(ref _model, value);
		}
	}

	/// <summary>
	/// Represents the reasoning group of the agent generation settings: the enable flag
	/// and the reasoning settings overriding the model's default settings.
	/// </summary>
	public class ReasoningOverrideSettings : NotifyPropertyChanged
	{
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
		/// The reasoning settings to use for the agent. These override the model's default
		/// settings if <see cref="EnableReasoningSettings"/> is <see langword="true"/>.
		/// </summary>
		public ReasoningSettings ReasoningSettings
		{
			get => _reasoningSettings;
			set => SetProperty(ref _reasoningSettings, value);
		}
	}

	/// <summary>
	/// Represents the temperature group of the agent generation settings: the enable flag
	/// and the temperature value overriding the model's default settings.
	/// </summary>
	public class TemperatureSettings : NotifyPropertyChanged
	{
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
		/// The temperature to use for the agent in range from 0 to 2. This overrides the
		/// model's default settings if <see cref="EnableTemperature"/> is <see langword="true"/>.
		/// </summary>
		public float Temperature
		{
			get => _temperature;
			set => SetProperty(ref _temperature, value);
		}
	}

	/// <summary>
	/// Represents the max tokens group of the agent generation settings: the enable flag
	/// and the maximum number of tokens overriding the model's default settings.
	/// </summary>
	public class MaxTokensSettings : NotifyPropertyChanged
	{
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
		/// The maximum number of tokens to generate for the agent. This overrides the
		/// model's default settings if <see cref="EnableMaxTokens"/> is <see langword="true"/>.
		/// </summary>
		public int MaxTokens
		{
			get => _maxTokens;
			set => SetProperty(ref _maxTokens, value);
		}
	}
}
