namespace LLMDesktopAssistant.Data.Connectors
{
	/// <summary>
	/// Represents an open database connection.
	/// </summary>
	public interface IDatabaseConnection : IAsyncDisposable
	{
		/// <summary>
		/// Gets the type of the database connector that created this connection.
		/// </summary>
		DatabaseConnectorType Type { get; }

		/// <summary>
		/// Tests the connection by opening and closing it.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		Task TestConnectionAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Executes a query and returns the result rows as formatted strings.
		/// </summary>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		Task<IEnumerable<string>> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default);
	}
}
