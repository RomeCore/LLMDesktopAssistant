using LLMDesktopAssistant.ApiKeys;
using LLMDesktopAssistant.Data.Connectors;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Represents a named database connection setting.
	/// </summary>
	public class DatabaseConnectionSetting : NotifyPropertyChanged
	{
		private string? _name;
		/// <summary>
		/// The name of the database connection.
		/// </summary>
		public string? Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private DatabaseConnectorType _connectorType;
		/// <summary>
		/// The type of database connector to use.
		/// </summary>
		public DatabaseConnectorType ConnectorType
		{
			get => _connectorType;
			set => SetProperty(ref _connectorType, value);
		}

		private bool _useEncryptedConnectionString;
		/// <summary>
		/// Gets or sets a value indicating whether the connection string is stored encrypted
		/// via <see cref="IApiKeyManagerService"/> and referenced by <see cref="EncryptedConnectionStringId"/>.
		/// </summary>
		public bool UseEncryptedConnectionString
		{
			get => _useEncryptedConnectionString;
			set => SetProperty(ref _useEncryptedConnectionString, value);
		}

		private string? _rawConnectionString;
		/// <summary>
		/// The raw connection string for the database.
		/// </summary>
		public string? RawConnectionString
		{
			get => _rawConnectionString;
			set => SetProperty(ref _rawConnectionString, value);
		}

		private Guid _encryptedConnectionStringId;
		/// <summary>
		/// The ID of the encrypted connection string. Resolved by <see cref="IApiKeyManagerService"/>.
		/// </summary>
		public Guid EncryptedConnectionStringId
		{
			get => _encryptedConnectionStringId;
			set => SetProperty(ref _encryptedConnectionStringId, value);
		}

		private bool _isEnabled = true;
		/// <summary>
		/// Whether the database connection is enabled or not. Used for convenience to disable certain settings without removing them.
		/// </summary>
		public bool IsEnabled
		{
			get => _isEnabled;
			set => SetProperty(ref _isEnabled, value);
		}

		private bool _isActive = false;
		/// <summary>
		/// Whether the database connection is currently active or not. Only ONE database connection can be active at a time.
		/// </summary>
		public bool IsActive
		{
			get => _isActive;
			set => SetProperty(ref _isActive, value);
		}

		/// <summary>
		/// Gets the connection string based on the current settings.
		/// </summary>
		public string? GetConnectionString(IApiKeyManagerService apiKeyManager) => UseEncryptedConnectionString
			? apiKeyManager.ResolveKey(EncryptedConnectionStringId)
			: RawConnectionString;
	}
}
