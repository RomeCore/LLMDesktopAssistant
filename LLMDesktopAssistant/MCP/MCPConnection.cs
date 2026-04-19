using System.IO;
using ModelContextProtocol.Client;

namespace LLMDesktopAssistant.MCP
{
	public class MCPConnection : IAsyncDisposable
	{
		public MCPServerInfo Info { get; }
		public McpClient Client { get; }

		private MCPConnection(MCPServerInfo mcpInfo, McpClient client)
		{
			Info = mcpInfo;
			Client = client;
		}

		/// <summary>
		/// Creates a new MCP connection based on the provided server information. This method initializes the appropriate transport mechanism (e.g., stdio, HTTP) based on the connection type specified in the <see cref="MCPServerInfo"/> and establishes a connection to the MCP server.
		/// </summary>
		/// <param name="mcpInfo"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public static async Task<MCPConnection> CreateAsync(MCPServerInfo mcpInfo, CancellationToken cancellationToken = default)
		{
			IClientTransport transport;
			switch (mcpInfo.ConnectionType)
			{
				case MCPConnectionType.Stdio:

					if (IsExecutableFile(mcpInfo.Endpoint))
					{
						var fullPath = Path.GetFullPath(mcpInfo.Endpoint);
						var directory = Path.GetDirectoryName(fullPath);
						transport = new StdioClientTransport(new StdioClientTransportOptions
						{
							Command = fullPath,
							WorkingDirectory = directory
						});
					}
					else
					{
						transport = new StdioClientTransport(new StdioClientTransportOptions
						{
							Command = mcpInfo.Endpoint
						});
					}
					break;
				case MCPConnectionType.Remote:
					transport = new HttpClientTransport(new HttpClientTransportOptions
					{
						Endpoint = new Uri(mcpInfo.Endpoint),
						Name = mcpInfo.Name
					});
					break;
				default:
					throw new InvalidOperationException($"Unsupported connection type: {mcpInfo.ConnectionType}");
			}
			var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
			return new MCPConnection(mcpInfo, client);
		}

		private static bool IsExecutableFile(string path)
		{
			return File.Exists(path) && Path.GetExtension(path).ToLower() == ".exe";
		}

		public async ValueTask DisposeAsync()
		{
			await Client.DisposeAsync();
		}
	}
}