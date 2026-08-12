namespace LLMDesktopAssistant.Data.Connectors
{
	/// <summary>
	/// Provides lazily created database connections cached by their connector type and
	/// connection string. The cache is application-wide so that multiple chats share a
	/// single connection to the same database (important for file-based databases such
	/// as SQLite, which lock their files while a connection is open).
	/// </summary>
	public interface IDatabaseConnectionCache : IAsyncDisposable
	{
		/// <summary>
		/// Gets the database connector types available on the current platform
		/// (derived from the registered <see cref="IDatabaseConnector"/> services).
		/// </summary>
		IEnumerable<DatabaseConnectorType> SupportedConnectors { get; }

		/// <summary>
		/// Gets a connection for the specified connector type and connection string,
		/// opening it lazily on the first use and reusing the cached connection afterwards.
		/// </summary>
		/// <param name="type">The type of the database connector to use.</param>
		/// <param name="connectionString">The connection string to connect to.</param>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>The cached or newly opened connection.</returns>
		Task<IDatabaseConnection> GetAsync(DatabaseConnectorType type, string connectionString, CancellationToken cancellationToken = default);

		/// <summary>
		/// Tests the specified connection string by opening a temporary connection.
		/// When the connection is already cached, the test succeeds without opening
		/// a second connection (which could lock a file-based database such as SQLite).
		/// </summary>
		/// <param name="type">The type of the database connector to use.</param>
		/// <param name="connectionString">The connection string to test.</param>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		Task TestAsync(DatabaseConnectorType type, string connectionString, CancellationToken cancellationToken = default);

		/// <summary>
		/// Closes and removes the connection for the specified connector type and
		/// connection string from the cache, if any.
		/// </summary>
		/// <param name="type">The type of the database connector.</param>
		/// <param name="connectionString">The connection string of the connection to close.</param>
		Task DisconnectAsync(DatabaseConnectorType type, string connectionString);

		/// <summary>
		/// Closes and removes all cached connections.
		/// </summary>
		Task DisconnectAllAsync();
	}
}
