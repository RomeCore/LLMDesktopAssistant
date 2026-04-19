using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Settings
{
	[ViewModelFor(typeof(ChatSettingsView))]
	public class ChatSettingsViewModel : ViewModelBase
	{
		public ChatSettings Settings { get; }
		public Chat Chat { get; }

		public ChatGeneralSettingsViewModel GeneralSettings { get; }
		public ChatPromptSettingsViewModel PromptSettings { get; }
		public ChatToolSettingsViewModel ToolSettings { get; }
		public ChatMCPSettingsViewModel McpSettings { get; }

		public ChatSettingsViewModel(ChatSettings settings, Chat chat)
		{
			Settings = settings;
			Chat = chat;

			GeneralSettings = new ChatGeneralSettingsViewModel(this);
			PromptSettings = new ChatPromptSettingsViewModel(this);
			ToolSettings = new ChatToolSettingsViewModel(this);
			McpSettings = new ChatMCPSettingsViewModel(this);
		}
	}
}