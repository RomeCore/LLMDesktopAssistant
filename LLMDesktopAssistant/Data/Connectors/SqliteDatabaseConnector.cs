using Microsoft.Data.Sqlite;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Data.Connectors
{
	/// <summary>
	/// A connector that creates connections to SQLite databases.
	/// </summary>
	[Service(typeof(IDatabaseConnector))]
	public class SqliteDatabaseConnector : IDatabaseConnector
	{
		/// <inheritdoc/>
		public DatabaseConnectorType Type => DatabaseConnectorType.SQLite;

		/// <inheritdoc/>
		public Task<IDatabaseConnection> ConnectAsync(string connectionString, CancellationToken cancellationToken = default)
		{
			var connection = new SqliteConnection(connectionString);
			return Task.FromResult<IDatabaseConnection>(new SqliteDatabaseConnection(connection));
		}

		private sealed class SqliteDatabaseConnection : IDatabaseConnection
		{
			private readonly SqliteConnection _connection;

			public SqliteDatabaseConnection(SqliteConnection connection)
			{
				_connection = connection;
			}

			/// <inheritdoc/>
			public DatabaseConnectorType Type => DatabaseConnectorType.SQLite;

			/// <inheritdoc/>
			public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
			{
				await _connection.OpenAsync(cancellationToken);
				await _connection.CloseAsync();
			}

			/// <inheritdoc/>
			public Task<IEnumerable<string>> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
			{
				throw new NotSupportedException("Query execution is not implemented yet.");
			}

			/// <inheritdoc/>
			public ValueTask DisposeAsync()
			{
				return _connection.DisposeAsync();
			}
		}
	}
}
