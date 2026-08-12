namespace LLMDesktopAssistant.Data.Connectors
{
	/// <summary>
	/// Manages the active database connection of a chat session: connects to named
	/// connections from the chat settings or to custom connection strings, and
	/// exposes the currently connected database to the database tools.
	/// </summary>
	public interface IDatabaseConnectionManager : IAsyncDisposable
	{
		/// <summary>
		/// Gets the currently connected database, or <see langword="null"/> if none is connected.
		/// </summary>
		IDatabaseConnection? Current { get; }

		/// <summary>
		/// Gets the human-readable description of the current connection
		/// (the name of the named connection or "custom (type)").
		/// </summary>
		string? CurrentDescription { get; }

		/// <summary>
		/// Connects to a database: either a named connection from the chat settings
		/// (when <paramref name="nameOrConnectionString"/> matches its name) or a custom
		/// connection string with the specified connector type.
		/// </summary>
		/// <param name="nameOrConnectionString">The name of the configured connection or a raw connection string.</param>
		/// <param name="connectorType">The connector type for a custom connection string: "sqlite" or "sqlserver". Ignored for named connections.</param>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>The newly opened connection.</returns>
		Task<IDatabaseConnection> ConnectAsync(string nameOrConnectionString, string? connectorType = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Disconnects the current connection, if any.
		/// </summary>
		void Disconnect();
	}
}
