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
		private ChatUserSettings _userSettings = new();
		/// <summary>
		/// Settings for the users interacting with the chat.
		/// </summary>
		public ChatUserSettings Users
		{
			get => _userSettings;
			set => SetProperty(ref _userSettings, value);
		}

		private ChatModelSettings _modelSettings = new();
		/// <summary>
		/// Settings related to language models used in chat.
		/// </summary>
		public ChatModelSettings Models
		{
			get => _modelSettings;
			set => SetProperty(ref _modelSettings, value);
		}

		private ChatAgentSettings _agentSettings = new();
		/// <summary>
		/// Settings related to chat agents.
		/// </summary>
		public ChatAgentSettings Agents
		{
			get => _agentSettings;
			set => SetProperty(ref _agentSettings, value);
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

		private ChatEnvironmentSettings _environmentSettings = new();
		/// <summary>
		/// Environment and working directory settings.
		/// </summary>
		public ChatEnvironmentSettings Environment
		{
			get => _environmentSettings;
			set => SetProperty(ref _environmentSettings, value);
		}

		private ChatToolsSettings _toolsSettings = new();
		/// <summary>
		/// Settings for tools and plugins used in the chat.
		/// </summary>
		public ChatToolsSettings Tools
		{
			get => _toolsSettings;
			set => SetProperty(ref _toolsSettings, value);
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