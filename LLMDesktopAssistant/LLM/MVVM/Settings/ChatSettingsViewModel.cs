using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Tools;

namespace LLMDesktopAssistant.LLM.Settings
{
	[ViewModelFor(typeof(ChatSettingsView))]
	public class ChatSettingsViewModel : ViewModelBase
	{
		public ChatSettings Settings { get; }
		public Chat Chat { get; }

		public ChatModelSettingsViewModel ModelSettings { get; }
		public ChatEnvironmentSettingsViewModel EnvironmentSettings { get; }
		public ChatPromptSettingsViewModel PromptSettings { get; }
		public ChatToolSettingsViewModel ToolSettings { get; }
		public ChatMCPSettingsViewModel McpSettings { get; }
		public ChatSummarizationSettingsViewModel SummarizationSettings { get; }
		public ChatLLMPropertiesSettingsViewModel LLMPropertiesSettings { get; }

		public ChatSettingsViewModel(ChatSettings settings, Chat chat)
		{
			Settings = settings;
			Chat = chat;

			ModelSettings = new ChatModelSettingsViewModel(settings.Models);
			SummarizationSettings = new ChatSummarizationSettingsViewModel(settings.Summarization);
			EnvironmentSettings = new ChatEnvironmentSettingsViewModel(settings.Environment);
			PromptSettings = new ChatPromptSettingsViewModel(settings.Prompts);
			ToolSettings = new ChatToolSettingsViewModel(settings.Tools, chat.Services.GetRequiredService<IToolsetBuildingService>());
			McpSettings = new ChatMCPSettingsViewModel(settings.Mcp, chat.Services.GetRequiredService<IMCPManagementService>());
			LLMPropertiesSettings = new ChatLLMPropertiesSettingsViewModel(settings.LLMProperties);
		}
	}
}
