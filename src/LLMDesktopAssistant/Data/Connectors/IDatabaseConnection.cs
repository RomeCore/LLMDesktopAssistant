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
		/// Executes a query and returns the structured result: the column headers and data rows
		/// for statements that return a result set, or the number of affected rows otherwise.
		/// </summary>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>The structured result of the query execution.</returns>
		Task<DatabaseQueryResult> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets a markdown-formatted description of the database schema: tables, views and their columns.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>The markdown-formatted schema description.</returns>
		Task<string> GetSchemaAsync(CancellationToken cancellationToken = default);
	}
}
