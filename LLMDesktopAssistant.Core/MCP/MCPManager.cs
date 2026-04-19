using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;
using Serilog;

namespace LLMDesktopAssistant.MCP
{
	/// <summary>
	/// The class for managing MCP connections.
	/// </summary>
	public static class MCPManager
	{
		private static readonly AsyncCache<Guid, MCPConnectionInfo> _connections =
			new(ConnectionFactory,
				slidingExpirationTime: TimeSpan.FromHours(1),
				cleanupInterval: TimeSpan.FromMinutes(10));

		private static async Task<MCPConnectionInfo> ConnectionFactory(Guid serverId)
		{
			var info = GetConfiguration().Servers.FirstOrDefault(s => s.Id == serverId);
			if (info == null)
			{
				Log.Error("Failed to find server information for ID: {ServerName}", serverId);
				throw new InvalidOperationException($"Server id:{serverId} not found in configuration.");
			}

			var connection = await MCPConnection.CreateAsync(info);
			var toolModule = await MCPToolModule.CreateAsync(connection);

			return new MCPConnectionInfo
			{
				ServerInfo = info,
				Connection = connection,
				ToolModule = toolModule
			};
		}

		/// <summary>
		/// Gets the configuration object that contains all registered MCP servers.
		/// </summary>
		/// <returns>The configuration object.</returns>
		public static MCPConfiguration GetConfiguration()
		{
			return SettingsManager.Get<MCPConfiguration>();
		}

		/// <summary>
		/// Checks if a connection to the specified MCP server exists.
		/// </summary>
		/// <param name="serverId">The ID of the MCP server.</param>
		/// <param name="connection">The connection information if it exists.</param>
		/// <returns>True if the connection exists; otherwise, false.</returns>
		public static bool CheckConnection(Guid serverId, out MCPConnectionInfo connection)
		{
			return _connections.TryGet(serverId, out connection);
		}

		/// <summary>
		/// Ensures that a connection to the specified MCP server exists. If not, it creates one.
		/// </summary>
		/// <param name="serverId">The ID of the MCP server.</param>
		/// <param name="cancellationToken">The cancellation token to use for the operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public static async Task<MCPConnectionInfo> EnsureConnectionAsync(Guid serverId, CancellationToken cancellationToken = default)
		{
			return await _connections.GetAsync(serverId, cancellationToken);
		}

		/// <summary>
		/// Closes and removes the connection to the specified MCP server.
		/// </summary>
		/// <param name="serverId">The ID of the MCP server.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public static async Task DisconnectAsync(Guid serverId)
		{
			await _connections.TryRemoveAsync(serverId);
		}
	}
}