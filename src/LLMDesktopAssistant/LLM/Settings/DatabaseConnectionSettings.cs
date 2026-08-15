using LLMDesktopAssistant.ApiKeys;
using LLMDesktopAssistant.Data.Connectors;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Represents the database connection configuration for a chat: a custom connection string
	/// and the list of named database connections.
	/// </summary>
	public class DatabaseConnectionSettings : NotifyPropertyChanged
	{
		private bool _isCustomActive;
		/// <summary>
		/// Gets or sets a value indicating whether the custom connection string is currently active.
		/// When <see langword="true"/>, <see cref="CustomConnectionString"/> takes precedence over
		/// the active named connection.
		/// </summary>
		public bool IsCustomActive
		{
			get => _isCustomActive;
			set => SetProperty(ref _isCustomActive, value);
		}

		private string? _customConnectionString;
		/// <summary>
		/// Gets or sets the custom connection string used when <see cref="IsCustomActive"/> is <see langword="true"/>.
		/// </summary>
		public string? CustomConnectionString
		{
			get => _customConnectionString;
			set => SetProperty(ref _customConnectionString, value);
		}

		private DatabaseConnectorType _customConnectorType = DatabaseConnectorType.SQLite;
		/// <summary>
		/// Gets or sets the type of the database connector used for <see cref="CustomConnectionString"/>.
		/// </summary>
		public DatabaseConnectorType CustomConnectorType
		{
			get => _customConnectorType;
			set => SetProperty(ref _customConnectorType, value);
		}

		private readonly RangeObservableCollection<DatabaseConnectionSetting> _items = [];
		/// <summary>
		/// The list of named database connections that can be used by the agent.
		/// </summary>
		public RangeObservableCollection<DatabaseConnectionSetting> Items
		{
			get => _items;
			set => _items.Reset(value);
		}

		/// <summary>
		/// Gets the effective connection string based on the current settings:
		/// the custom connection string when <see cref="IsCustomActive"/> is <see langword="true"/>,
		/// otherwise the connection string of the active named connection.
		/// </summary>
		/// <param name="apiKeyManager">The API key manager used to resolve encrypted connection strings.</param>
		/// <returns>The effective connection string, or <see langword="null"/> if none is configured.</returns>
		public string? GetConnectionString(IApiKeyManagerService apiKeyManager) =>
			IsCustomActive && !string.IsNullOrEmpty(CustomConnectionString) ? CustomConnectionString :
				Items.FirstOrDefault(w => w.IsEnabled && w.IsActive)?.GetConnectionString(apiKeyManager);
	}
}
