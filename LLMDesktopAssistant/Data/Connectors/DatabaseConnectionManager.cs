using System.Collections.Immutable;
using LLMDesktopAssistant.ApiKeys;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.Data.Connectors
{
	/// <summary>
	/// Default implementation of <see cref="IDatabaseConnectionManager"/>.
	/// </summary>
	[ChatService(typeof(IDatabaseConnectionManager))]
	public class DatabaseConnectionManager : IDatabaseConnectionManager
	{
		private readonly Chat _chat;
		private readonly IApiKeyManagerService _apiKeyManager;
		private readonly ImmutableList<IDatabaseConnector> _connectors;
		private IDatabaseConnection? _current;
		private string? _currentDescription;

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseConnectionManager"/> class.
		/// </summary>
		/// <param name="chat">The chat whose settings contain the database connections.</param>
		/// <param name="apiKeyManager">The API key manager used to resolve encrypted connection strings.</param>
		/// <param name="connectors">The registered database connectors.</param>
		public DatabaseConnectionManager(Chat chat, IApiKeyManagerService apiKeyManager, IEnumerable<IDatabaseConnector> connectors)
		{
			_chat = chat;
			_apiKeyManager = apiKeyManager;
			_connectors = connectors.ToImmutableList();
		}

		/// <inheritdoc/>
		public IDatabaseConnection? Current => _current;

		/// <inheritdoc/>
		public string? CurrentDescription => _currentDescription;

		/// <inheritdoc/>
		public async Task<IDatabaseConnection> ConnectAsync(string nameOrConnectionString, string? connectorType = null, CancellationToken cancellationToken = default)
		{
			var settings = _chat.Settings.Databases.GetEffectiveDatabaseConnection();
			var named = settings.Items.FirstOrDefault(i => i.IsEnabled &&
				string.Equals(i.Name, nameOrConnectionString, StringComparison.OrdinalIgnoreCase));

			DatabaseConnectorType type;
			string? connectionString;
			string description;

			if (named is not null)
			{
				type = named.ConnectorType;
				connectionString = named.GetConnectionString(_apiKeyManager);
				description = named.Name!;
			}
			else
			{
				type = ParseConnectorType(connectorType ?? nameof(DatabaseConnectorType.SQLite));
				connectionString = nameOrConnectionString;
				description = $"custom ({type})";
			}

			if (string.IsNullOrWhiteSpace(connectionString))
				throw new InvalidOperationException($"Connection string for '{nameOrConnectionString}' is empty.");

			var connector = _connectors.FirstOrDefault(c => c.Type == type)
				?? throw new NotSupportedException($"No connector registered for database type '{type}'.");

			Disconnect();

			var connection = await connector.ConnectAsync(connectionString, cancellationToken);
			_current = connection;
			_currentDescription = description;
			return connection;
		}

		/// <inheritdoc/>
		public void Disconnect()
		{
			if (_current is not null)
			{
				_current.DisposeAsync().AsTask().GetAwaiter().GetResult();
				_current = null;
				_currentDescription = null;
			}
		}

		/// <inheritdoc/>
		public ValueTask DisposeAsync()
		{
			if (_current is null)
				return ValueTask.CompletedTask;

			var current = _current;
			_current = null;
			_currentDescription = null;
			return current.DisposeAsync();
		}

		private static DatabaseConnectorType ParseConnectorType(string connectorType)
		{
			if (Enum.TryParse<DatabaseConnectorType>(connectorType, ignoreCase: true, out var parsed))
				return parsed;
			throw new ArgumentException(
				$"Unknown connector type '{connectorType}'. Expected one of: {string.Join(", ", Enum.GetNames<DatabaseConnectorType>())}.",
				nameof(connectorType));
		}
	}
}
