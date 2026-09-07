using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Database connection settings for a chat.
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.Databases))]
	public partial class ChatDatabaseSettings : ChatSettingsCategoryBase
	{
		/// <summary>
		/// Gets or sets the database connection configuration for the chat.
		/// </summary>
		[InheritedChatSetting]
		public DatabaseConnectionSettings DatabaseConnection
		{
			get => field ??= new();
			set => SetProperty(ref field, value);
		}
	}
}
