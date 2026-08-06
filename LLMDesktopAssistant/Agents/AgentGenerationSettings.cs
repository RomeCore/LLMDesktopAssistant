using LLMDesktopAssistant.SourceGenerators;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes an agent's generation settings.
	/// All groups are resolved through their effective (inherited) scope, selected via
	/// the inheritance level combo boxes in the view.
	/// </summary>
	[SettingsRoute(nameof(ChatAgentDescriptor.Generation))]
	public partial class AgentGenerationSettings : AgentSettingsCategoryBase
	{
		private CustomModelSettings _customModel = new();
		/// <summary>
		/// Gets or sets the custom model group of the agent: the enable flag and the model override.
		/// </summary>
		[InheritedChatAgentSetting]
		public CustomModelSettings CustomModel
		{
			get => _customModel;
			set => SetProperty(ref _customModel, value);
		}

		private ReasoningOverrideSettings _reasoning = new();
		/// <summary>
		/// Gets or sets the reasoning group of the agent: the enable flag and the reasoning settings override.
		/// </summary>
		[InheritedChatAgentSetting]
		public ReasoningOverrideSettings Reasoning
		{
			get => _reasoning;
			set => SetProperty(ref _reasoning, value);
		}

		private TemperatureSettings _temperature = new();
		/// <summary>
		/// Gets or sets the temperature group of the agent: the enable flag and the temperature override.
		/// </summary>
		[InheritedChatAgentSetting]
		public TemperatureSettings Temperature
		{
			get => _temperature;
			set => SetProperty(ref _temperature, value);
		}

		private MaxTokensSettings _maxTokens = new();
		/// <summary>
		/// Gets or sets the max tokens group of the agent: the enable flag and the max tokens override.
		/// </summary>
		[InheritedChatAgentSetting]
		public MaxTokensSettings MaxTokens
		{
			get => _maxTokens;
			set => SetProperty(ref _maxTokens, value);
		}

		private RangeObservableCollection<AdditionalParameter> _additionalParameters = [];
		/// <summary>
		/// The additional parameters to use for the agent. These are represented in a key-value format and passed to API.
		/// </summary>
		[InheritedChatAgentSetting]
		public RangeObservableCollection<AdditionalParameter> AdditionalParameters
		{
			get => _additionalParameters;
			set => _additionalParameters.Reset(value);
		}
	}
}
