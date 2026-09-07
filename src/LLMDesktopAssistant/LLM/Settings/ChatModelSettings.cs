using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Settings related to language models used in chat.
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.Models))]
	public partial class ChatModelSettings : ChatSettingsCategoryBase
	{
		/// <summary>
		/// Gets or sets the model selection group for this chat.
		/// </summary>
		[InheritedChatSetting]
		public ModelSelectionSettings Selection
		{
			get => field ??= new();
			set => SetProperty(ref field, value);
		}
	}
}
