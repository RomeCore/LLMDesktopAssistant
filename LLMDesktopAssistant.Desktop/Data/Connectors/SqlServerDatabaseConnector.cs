using LLMDesktopAssistant.Data.Connectors;
using LLMDesktopAssistant.Services;
using Microsoft.Data.SqlClient;

namespace LLMDesktopAssistant.Desktop.Data.Connectors
{
	/// <summary>
	/// A connector that creates connections to SQL Server databases.
	/// </summary>
	[Service(typeof(IDatabaseConnector))]
	public class SqlServerDatabaseConnector : IDatabaseConnector
	{
		/// <inheritdoc/>
		public DatabaseConnectorType Type => DatabaseConnectorType.SQLServer;

		/// <inheritdoc/>
		public Task<IDatabaseConnection> ConnectAsync(string connectionString, CancellationToken cancellationToken = default)
		{
			var connection = new SqlConnection(connectionString);
			return Task.FromResult<IDatabaseConnection>(new SqlServerDatabaseConnection(connection));
		}

		private sealed class SqlServerDatabaseConnection : IDatabaseConnection
		{
			private readonly SqlConnection _connection;

			public SqlServerDatabaseConnection(SqlConnection connection)
			{
				_connection = connection;
			}

			/// <inheritdoc/>
			public DatabaseConnectorType Type => DatabaseConnectorType.SQLServer;

			/// <inheritdoc/>
			public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
			{
				await _connection.OpenAsync(cancellationToken);
				_connection.Close();
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
