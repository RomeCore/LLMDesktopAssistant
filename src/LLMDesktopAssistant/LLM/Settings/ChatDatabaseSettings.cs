using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Database connection settings for a chat.
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.Databases))]
	public partial class ChatDatabaseSettings : ChatSettingsCategoryBase
	{
		private DatabaseConnectionSettings _databaseConnection = new();
		/// <summary>
		/// Gets or sets the database connection configuration for the chat.
		/// </summary>
		[InheritedChatSetting]
		public DatabaseConnectionSettings DatabaseConnection
		{
			get => _databaseConnection;
			set => SetProperty(ref _databaseConnection, value);
		}
	}
}
