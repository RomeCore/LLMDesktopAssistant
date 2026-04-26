using LLMDesktopAssistant.Settings;
using RCLargeLanguageModels;
using System.Collections;
using System.IO;
using System.Runtime;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Class representing the settings for a chat session.
	/// </summary>
	public class ChatSettings : SettingsObject
	{
		private ChatModelSettings _modelSettings = new();
		/// <summary>
		/// Settings related to language models used in chat.
		/// </summary>
		public ChatModelSettings Models
		{
			get => _modelSettings;
			set => SetProperty(ref _modelSettings, value);
		}

		private ChatLLMPropertiesSettings _llmPropertiesSettings = new();
		/// <summary>
		/// Settings related to properties that can be applied to chat LLM.
		/// </summary>
		public ChatLLMPropertiesSettings LLMProperties
		{
			get => _llmPropertiesSettings;
			set => SetProperty(ref _llmPropertiesSettings, value);
		}

		private ChatSummarizationSettings _summarizationSettings = new();
		/// <summary>
		/// Settings for conversation auto-summarization.
		/// </summary>
		public ChatSummarizationSettings Summarization
		{
			get => _summarizationSettings;
			set => SetProperty(ref _summarizationSettings, value);
		}

		private ChatPromptSettings _promptSettings = new();
		/// <summary>
		/// Settings for system prompts and persona configuration.
		/// </summary>
		public ChatPromptSettings Prompts
		{
			get => _promptSettings;
			set => SetProperty(ref _promptSettings, value);
		}

		private ChatEnvironmentSettings _environmentSettings = new();
		/// <summary>
		/// Environment and working directory settings.
		/// </summary>
		public ChatEnvironmentSettings Environment
		{
			get => _environmentSettings;
			set => SetProperty(ref _environmentSettings, value);
		}

		private ChatToolSettings _toolSettings = new();
		/// <summary>
		/// Settings for tools and function calling.
		/// </summary>
		public ChatToolSettings Tools
		{
			get => _toolSettings;
			set => SetProperty(ref _toolSettings, value);
		}

		private ChatMcpSettings _mcpSettings = new();
		/// <summary>
		/// Settings for MCP (Model Context Protocol) servers.
		/// </summary>
		public ChatMcpSettings Mcp
		{
			get => _mcpSettings;
			set => SetProperty(ref _mcpSettings, value);
		}
	}
}