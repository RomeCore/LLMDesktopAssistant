namespace LLMDesktopAssistant.Data.Connectors
{
	/// <summary>
	/// Manages the active database connection of a chat session: switches the active
	/// connection string in the chat settings and provides lazily created, cached
	/// database connections to the database tools.
	/// </summary>
	public interface IDatabaseConnectionManager : IAsyncDisposable
	{
		/// <summary>
		/// Gets a value indicating whether a non-empty connection string is currently
		/// active in the chat settings.
		/// </summary>
		bool IsActiveConfigured { get; }

		/// <summary>
		/// Gets the database connector types available on the current platform
		/// (derived from the registered <see cref="IDatabaseConnector"/> services).
		/// </summary>
		IEnumerable<DatabaseConnectorType> SupportedConnectors { get; }

		/// <summary>
		/// Activates a database connection without actually connecting: either a named
		/// connection from the chat settings (when <paramref name="nameOrConnectionString"/>
		/// matches its name) or a custom connection string with the specified connector type.
		/// </summary>
		/// <param name="nameOrConnectionString">The name of the configured connection or a raw connection string.</param>
		/// <param name="connectorType">The connector type for a custom connection string. Ignored for named connections.</param>
		/// <returns>The description of the activated connection (the connection name or "custom (type)").</returns>
		string Activate(string nameOrConnectionString, DatabaseConnectorType? connectorType = null);

		/// <summary>
		/// Gets the connection for the active connection string, opening it lazily on the
		/// first use and reusing the cached connection afterwards.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>The active database connection.</returns>
		Task<IDatabaseConnection> GetCurrentAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Closes and removes the connection for the active connection string from the cache, if any.
		/// </summary>
		Task DisconnectAsync();

		/// <summary>
		/// Closes and removes all cached connections.
		/// </summary>
		Task DisconnectAllAsync();
	}
}
