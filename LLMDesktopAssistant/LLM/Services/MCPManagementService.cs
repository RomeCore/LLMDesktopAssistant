using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.MCP;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels;
using Serilog;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(IMCPManagementService))]
	public class MCPManagementService(
		Chat chat
		) : IMCPManagementService
	{
		private MCPConnectionInfo[] _usedConnections = [];

		public async Task EnsureCurrentMCPConnectionsAsync(CancellationToken cancellationToken = default)
		{
			if (!chat.Settings.Mcp.EnableMcp)
			{
				_usedConnections = [];
				return;
			}

			var usedServerIds = chat.Settings.Mcp.UsedMcpServers
				.Intersect(SettingsManager.Get<MCPConfiguration>().Servers.Select(s => s.Id));

			var usedConnectionTasks = usedServerIds.Select(id => MCPManager.EnsureConnectionAsync(id, cancellationToken));
			_usedConnections = await Task.WhenAll(usedConnectionTasks);
		}

		public MCPToolModule[] GetMCPTools()
		{
			if (!chat.Settings.Mcp.EnableMcp)
				return [];

			return _usedConnections.Select(c => c.ToolModule).ToArray();
		}
	}
}