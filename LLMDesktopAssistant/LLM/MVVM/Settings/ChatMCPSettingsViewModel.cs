using Avalonia.Collections;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.MCP;
using LLMDesktopAssistant.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace LLMDesktopAssistant.LLM.Settings
{
	public class MCPServerSelectionViewModel : ViewModelBase
	{
		private readonly MCPServerInfo _server;
		private readonly ChatMCPSettingsViewModel _settingsVm;

		public string Name => _server.Name;
		public string Endpoint => _server.Endpoint;
		public MCPConnectionType ConnectionType => _server.ConnectionType;

		public MCPServerSelectionViewModel(MCPServerInfo server, ChatMCPSettingsViewModel settingsVm)
		{
			_server = server;
			_settingsVm = settingsVm;
		}

		public bool IsEnabled
		{
			get => _settingsVm.McpSettings.UsedMcpServers.Contains(_server.Id);
			set
			{
				if (value)
				{
					if (!_settingsVm.McpSettings.UsedMcpServers.Contains(_server.Id))
					{
						_settingsVm.McpSettings.UsedMcpServers.Add(_server.Id);
						_settingsVm.EnsureMCPServers();
					}
				}
				else
				{
					if (_settingsVm.McpSettings.UsedMcpServers.Remove(_server.Id))
					{
						_settingsVm.EnsureMCPServers();
					}
				}

				RaisePropertyChanged(nameof(IsEnabled));
			}
		}
	}

	[ViewModelFor(typeof(ChatMCPSettingsView))]
	public class ChatMCPSettingsViewModel : ViewModelBase
	{
		private readonly IMCPManagementService _mcpManagementService;
		public ChatMcpSettings McpSettings { get; }

		private AvaloniaList<MCPServerSelectionViewModel> _mcpServers = [];
		public ICollection<MCPServerSelectionViewModel> McpServers
		{
			get => _mcpServers;
			set
			{
				_mcpServers.Clear();
				_mcpServers.AddRange(value);
			}
		}

		public ChatMCPSettingsViewModel(ChatMcpSettings settings, IMCPManagementService mcpManagementService)
		{
			_mcpManagementService = mcpManagementService;
			McpSettings = settings;

			var mcpConfig = MCPManager.GetConfiguration();
			McpServers = mcpConfig.Servers
				.Select(s => new MCPServerSelectionViewModel(s, this))
				.ToImmutableList();

			EnsureMCPServers();
		}

		private bool _ensuringMcp = false;
		public async void EnsureMCPServers()
		{
			if (_ensuringMcp)
				return;
			_ensuringMcp = true;

			try
			{
				await _mcpManagementService.EnsureCurrentMCPConnectionsAsync();
			}
			catch
			{

			}
			finally
			{
				_ensuringMcp = false;
			}
		}
	}
}