using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Settings for MCP (Model Context Protocol) servers.
	/// </summary>
	public class ChatMcpSettings : ChatSettingsCategoryBase
	{
		private bool _enableMcp = true;
		/// <summary>
		/// Whether to use the MCP servers for additional tools and resources.
		/// </summary>
		public bool EnableMcp
		{
			get => _enableMcp;
			set => SetProperty(ref _enableMcp, value);
		}

		private readonly RangeObservableCollection<Guid> _usedMcpServers = [];
		/// <summary>
		/// Gets or sets the used MCP server Ids.
		/// </summary>
		public ICollection<Guid> UsedMcpServers
		{
			get => _usedMcpServers;
			set => _usedMcpServers.Reset(value);
		}
	}
}