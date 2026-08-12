using System.Collections.Immutable;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Data.Connectors
{
	/// <summary>
	/// Default implementation of <see cref="IDatabaseConnectionCache"/> backed by an
	/// <see cref="AsyncCache{TKey, TValue}"/> keyed by the connector type and the
	/// connection string.
	/// </summary>
	[Service(typeof(IDatabaseConnectionCache))]
	public class DatabaseConnectionCache : IDatabaseConnectionCache
	{
		private readonly ImmutableList<IDatabaseConnector> _connectors;
		private readonly ImmutableArray<DatabaseConnectorType> _supportedConnectors;
		private readonly AsyncCache<(DatabaseConnectorType Type, string ConnectionString), IDatabaseConnection> _cache;

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseConnectionCache"/> class.
		/// </summary>
		/// <param name="connectors">The registered database connectors.</param>
		public DatabaseConnectionCache(IEnumerable<IDatabaseConnector> connectors)
		{
			_connectors = connectors.ToImmutableList();
			_supportedConnectors = _connectors.Select(c => c.Type).Distinct().OrderBy(t => (int)t).ToImmutableArray();
			_cache = new AsyncCache<(DatabaseConnectorType, string), IDatabaseConnection>(CreateConnectionAsync,
				slidingExpirationTime: TimeSpan.FromHours(1),
				cleanupInterval: TimeSpan.FromMinutes(10));
		}

		/// <inheritdoc/>
		public IEnumerable<DatabaseConnectorType> SupportedConnectors => _supportedConnectors;

		/// <inheritdoc/>
		public Task<IDatabaseConnection> GetAsync(DatabaseConnectorType type, string connectionString, CancellationToken cancellationToken = default)
		{
			return _cache.GetAsync((type, connectionString), cancellationToken);
		}

		/// <inheritdoc/>
		public async Task TestAsync(DatabaseConnectorType type, string connectionString, CancellationToken cancellationToken = default)
		{
			var key = (type, connectionString);
			if (_cache.TryGet(key, out _))
				return;

			var connector = _connectors.FirstOrDefault(c => c.Type == type)
				?? throw new NotSupportedException($"No connector registered for database type '{type}'.");

			await using var connection = await connector.ConnectAsync(connectionString, cancellationToken);
			await connection.TestConnectionAsync(cancellationToken);
		}

		/// <inheritdoc/>
		public async Task DisconnectAsync(DatabaseConnectorType type, string connectionString)
		{
			await _cache.TryRemoveAsync((type, connectionString));
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
	}
}
