using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Settings for conversation auto-summarization.
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.Summarization))]
	public partial class ChatSummarizationSettings : ChatSettingsCategoryBase
	{
		private bool _summarizationEnabled = false;
		/// <summary>
		/// Whether auto-summarization is enabled.
		/// Auto summarization triggers when total usage tokens exceeds a certain threshold (<see cref="SummarizationOptionsSettings.SummarizationTriggerTokens"/>).
		/// </summary>
		public bool AutoSummarizationEnabled
		{
			get => _summarizationEnabled;
			set => SetProperty(ref _summarizationEnabled, value);
		}

		private SummarizationOptionsSettings _options = new();
		/// <summary>
		/// Gets or sets the auto-summarization options group for this chat.
		/// </summary>
		[InheritedChatSetting]
		public SummarizationOptionsSettings Options
		{
			get => _options;
			set => SetProperty(ref _options, value);
		}
	}
}
