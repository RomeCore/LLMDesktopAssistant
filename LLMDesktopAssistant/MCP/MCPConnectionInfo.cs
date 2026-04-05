namespace LLMDesktopAssistant.MCP
{
	/// <summary>
	/// The complete connection information for the connected MCP server.
	/// </summary>
	public class MCPConnectionInfo : IAsyncDisposable
	{
		/// <summary>
		/// The server information for the connected MCP server.
		/// </summary>
		public required MCPServerInfo ServerInfo { get; init; }

		/// <summary>
		/// The connection to the MCP server.
		/// </summary>
		public required MCPConnection Connection { get; init; }

		/// <summary>
		/// The tool module that contains tools that MCP server has been declared.
		/// </summary>
		public required MCPToolModule ToolModule { get; init; }

		public async ValueTask DisposeAsync()
		{
			await Connection.DisposeAsync();
			await ToolModule.DisposeAsync();
		}
	}
}