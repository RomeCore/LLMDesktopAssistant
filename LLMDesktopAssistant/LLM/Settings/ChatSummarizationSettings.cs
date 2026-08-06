using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Settings for conversation auto-summarization.
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.Summarization))]
	public partial class ChatSummarizationSettings : ChatSettingsCategoryBase
	{
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
