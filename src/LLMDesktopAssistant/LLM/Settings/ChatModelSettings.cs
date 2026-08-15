using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Settings related to language models used in chat.
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.Models))]
	public partial class ChatModelSettings : ChatSettingsCategoryBase
	{
		private ModelSelectionSettings _selection = new();
		/// <summary>
		/// Gets or sets the model selection group for this chat.
		/// </summary>
		[InheritedChatSetting]
		public ModelSelectionSettings Selection
		{
			get => _selection;
			set => SetProperty(ref _selection, value);
		}
	}
}
