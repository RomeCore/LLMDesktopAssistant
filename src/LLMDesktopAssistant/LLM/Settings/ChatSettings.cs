using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Class representing the settings for a chat session.
	/// </summary>
	[SettingsObject("chat")]
	public class ChatSettings : SettingsObject
	{
		private ChatAgentDescriptor _inheritedAgentSettings = new();
		/// <summary>
		/// Gets or sets the agent settings that will be used for inherited agent settings.
		/// Example: agent inherits chat's tool settings.
		/// </summary>
		public ChatAgentDescriptor InheritedAgentSettings
		{
			get => _inheritedAgentSettings;
			set => SetProperty(ref _inheritedAgentSettings, value);
		}

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

		private ChatMemorySettings _memorySettings = new();
		/// <summary>
		/// Settings for memory management in chat sessions.
		/// </summary>
		public ChatMemorySettings Memory
		{
			get => _memorySettings;
			set => SetProperty(ref _memorySettings, value);
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

		private ChatDatabaseSettings _databaseSettings = new();
		/// <summary>
		/// Settings related to database connections.
		/// </summary>
		public ChatDatabaseSettings Databases
		{
			get => _databaseSettings;
			set => SetProperty(ref _databaseSettings, value);
		}


		private ChatToolSettings _toolsSettings = new();
		/// <summary>
		/// Settings for tools and plugins used in the chat.
		/// </summary>
		public ChatToolSettings Tools
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

		private ChatSkillSettings _skillSettings = new();
		/// <summary>
		/// Settings related to skills.
		/// </summary>
		public ChatSkillSettings Skills
		{
			get => _skillSettings;
			set => SetProperty(ref _skillSettings, value);
		}

		private ChatSubAgentSettings _subAgentSettings = new();
		/// <summary>
		/// Settings related to sub-agents.
		/// </summary>
		public ChatSubAgentSettings SubAgents
		{
			get => _subAgentSettings;
			set => SetProperty(ref _subAgentSettings, value);
		}
	}
}