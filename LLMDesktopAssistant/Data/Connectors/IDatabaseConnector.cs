namespace LLMDesktopAssistant.Data.Connectors
{
	public interface IDatabaseConnector
	{
		DatabaseConnectorType Type { get; }

		Task<IDatabaseConnection> ConnectAsync(string connectionString, CancellationToken cancellationToken = default);
	}
}
