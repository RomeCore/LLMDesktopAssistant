using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Settings for conversation auto-summarization.
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.Memory))]
	public partial class ChatMemorySettings : ChatSettingsCategoryBase
	{
		private MemorySettings _memoryOptions = new();
		/// <summary>
		/// Options for enabling and managing conversation memory.
		/// </summary>
		[InheritedChatSetting]
		public MemorySettings MemoryOptions
		{
			get => _memoryOptions;
			set => SetProperty(ref _memoryOptions, value);
		}

		private SummarizationOptionsSettings _summarization = new();
		/// <summary>
		/// Gets or sets the auto-summarization options group for this chat.
		/// </summary>
		[InheritedChatSetting]
		public SummarizationOptionsSettings Summarization
		{
			get => _summarization;
			set => SetProperty(ref _summarization, value);
		}
	}
}
