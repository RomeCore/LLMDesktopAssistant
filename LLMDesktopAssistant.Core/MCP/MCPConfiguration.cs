using LLMDesktopAssistant.Core.Settings;
using LLMDesktopAssistant.Core.Utils;

namespace LLMDesktopAssistant.Core.MCP
{
	/// <summary>
	/// Represents the configuration settings for MCP (Model Context Protocol) servers within the application.
	/// </summary>
	public class MCPConfiguration : SettingsObject
	{
		private readonly RangeObservableCollection<MCPServerInfo> _servers = [];
		/// <summary>
		/// Gest or sets the list of MCP servers configured in the application. Each server configuration includes details such as the connection type, endpoint, and a user-friendly name.
		/// </summary>
		public ICollection<MCPServerInfo> Servers
		{
			get => _servers;
			set => _servers.Reset(value);
		}
	}
}