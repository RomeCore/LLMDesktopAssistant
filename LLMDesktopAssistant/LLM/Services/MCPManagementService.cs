using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.MCP;
using LLMDesktopAssistant.Settings;
using RCLargeLanguageModels;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(IMCPManagementService))]
	public class MCPManagementService(
		IChatSettingsService chatSettings
		) : IMCPManagementService
	{
		private MCPConnectionInfo[] _usedConnections = [];

		public bool HasMCPConnections()
		{
			if (!chatSettings.Settings.Mcp.EnableMcp)
				return false;

			var usedServerIds = chatSettings.Settings.Mcp.GetEffectiveUsedMcpServers()
				.Intersect(SettingsManager.Get<MCPConfiguration>().Servers.Select(s => s.Id));

			return usedServerIds.Any();
		}

		public async Task EnsureCurrentMCPConnectionsAsync(CancellationToken cancellationToken = default)
		{
			if (!chatSettings.Settings.Mcp.EnableMcp)
			{
				_usedConnections = [];
				return;
			}

			var usedServerIds = chatSettings.Settings.Mcp.GetEffectiveUsedMcpServers()
				.Intersect(SettingsManager.Get<MCPConfiguration>().Servers.Select(s => s.Id));

			var usedConnectionTasks = usedServerIds.Select(id => MCPManager.EnsureConnectionAsync(id, cancellationToken));
			_usedConnections = await Task.WhenAll(usedConnectionTasks);
		}

		public MCPToolModule[] GetMCPTools()
		{
			if (!chatSettings.Settings.Mcp.EnableMcp)
				return [];

			return _usedConnections.Select(c => c.ToolModule).ToArray();
		}
	}
}