namespace LLMDesktopAssistant.Core.MCP
{
	/// <summary>
	/// Defines the types of connections that can be used to communicate with an MCP server.
	/// </summary>
	public enum MCPConnectionType
	{
		/// <summary>
		/// The connection type is not defined. This value can be used as a default or placeholder until a valid connection type is set.
		/// </summary>
		Undefined = 0,

		/// <summary>
		/// Use standard input/output streams to communicate with the MCP server process. This is suitable for local processes that can be launched by the application.
		/// </summary>
		Stdio,

		/// <summary>
		/// Use remote communication protocols (e.g., HTTP, WebSocket) to connect to an MCP server that is running on a different machine or as a separate service. This allows for more flexible deployment scenarios, such as connecting to cloud-hosted MCP servers or services running in containers.
		/// </summary>
		Remote
	}
}