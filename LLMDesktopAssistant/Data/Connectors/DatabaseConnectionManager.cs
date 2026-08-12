using System.Collections.Immutable;
using LLMDesktopAssistant.ApiKeys;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Data.Connectors
{
	/// <summary>
	/// Default implementation of <see cref="IDatabaseConnectionManager"/>. Switching the
	/// active database only updates the chat settings; the actual connections are opened
	/// lazily on first use and cached by their connector type and connection string.
	/// </summary>
	[ChatService(typeof(IDatabaseConnectionManager))]
	public class DatabaseConnectionManager : IDatabaseConnectionManager
	{
		private readonly Chat _chat;
		private readonly IApiKeyManagerService _apiKeyManager;
		private readonly ImmutableList<IDatabaseConnector> _connectors;
		private readonly ImmutableArray<DatabaseConnectorType> _supportedConnectors;
		private readonly AsyncCache<(DatabaseConnectorType Type, string ConnectionString), IDatabaseConnection> _cache;

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
			_supportedConnectors = _connectors.Select(c => c.Type).Distinct().OrderBy(t => (int)t).ToImmutableArray();
			_cache = new AsyncCache<(DatabaseConnectorType, string), IDatabaseConnection>(CreateConnectionAsync,
				slidingExpirationTime: TimeSpan.FromHours(1),
				cleanupInterval: TimeSpan.FromMinutes(10));
		}

		/// <inheritdoc/>
		public IEnumerable<DatabaseConnectorType> SupportedConnectors => _supportedConnectors;

		/// <inheritdoc/>
		public bool IsActiveConfigured
		{
			get
			{
				var active = ResolveActive();
				return active is not null && !string.IsNullOrWhiteSpace(active.Value.ConnectionString);
			}
		}

		/// <inheritdoc/>
		public string Activate(string nameOrConnectionString, DatabaseConnectorType? connectorType = null)
		{
			var settings = _chat.Settings.Databases.GetEffectiveDatabaseConnection();
			var named = settings.Items.FirstOrDefault(i => i.IsEnabled &&
				string.Equals(i.Name, nameOrConnectionString, StringComparison.OrdinalIgnoreCase));

			if (named is not null)
			{
				settings.IsCustomActive = false;

				// Prevent activating multiple named connections with the same name.
				bool onceFlag = true;
				foreach (var item in settings.Items)
				{
					item.IsActive = item == named && onceFlag;
					if (item.IsActive)
						onceFlag = false;
				}

				return named.Name!;
			}

			settings.IsCustomActive = true;
			settings.CustomConnectionString = nameOrConnectionString;
			settings.CustomConnectorType = connectorType ?? DatabaseConnectorType.SQLite;
			foreach (var item in settings.Items)
				item.IsActive = false;

			return $"custom ({settings.CustomConnectorType})";
		}

		/// <inheritdoc/>
		public async Task<IDatabaseConnection> GetCurrentAsync(CancellationToken cancellationToken = default)
		{
			var active = ResolveActive()
				?? throw new InvalidOperationException("No active database connection. Call db-connect first.");

			var (type, connectionString, _) = active;
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new InvalidOperationException($"Connection string for '{active.Description}' is empty.");

			return await _cache.GetAsync((type, connectionString), cancellationToken);
		}

		/// <inheritdoc/>
		public async Task DisconnectAsync()
		{
			var active = ResolveActive();
			if (active is null || string.IsNullOrWhiteSpace(active.Value.ConnectionString))
				return;

			await _cache.TryRemoveAsync((active.Value.Type, active.Value.ConnectionString));
		}

		/// <inheritdoc/>
		public async Task DisconnectAllAsync()
		{
			foreach (var key in _cache.Keys)
				await _cache.TryRemoveAsync(key);
		}

		/// <inheritdoc/>
		public async ValueTask DisposeAsync()
		{
			await DisconnectAllAsync();
			_cache.Dispose();
		}

		private Task<IDatabaseConnection> CreateConnectionAsync((DatabaseConnectorType Type, string ConnectionString) key, CancellationToken cancellationToken)
		{
			var connector = _connectors.FirstOrDefault(c => c.Type == key.Type)
				?? throw new NotSupportedException($"No connector registered for database type '{key.Type}'.");

			return connector.ConnectAsync(key.ConnectionString, cancellationToken);
		}

		private (DatabaseConnectorType Type, string ConnectionString, string Description)? ResolveActive()
		{
			var settings = _chat.Settings.Databases.GetEffectiveDatabaseConnection();

			if (settings.IsCustomActive && !string.IsNullOrWhiteSpace(settings.CustomConnectionString))
				return (settings.CustomConnectorType, settings.CustomConnectionString, $"custom ({settings.CustomConnectorType})");

			var named = settings.Items.FirstOrDefault(i => i.IsEnabled && i.IsActive);
			if (named is not null)
			{
				var connectionString = named.GetConnectionString(_apiKeyManager);
				if (!string.IsNullOrWhiteSpace(connectionString))
					return (named.ConnectorType, connectionString, named.Name!);
			}

			return null;
		}
	}
}
