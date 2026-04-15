using Avalonia.Collections;
using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.LLM.Services;
using LLMDesktopAssistant.Core.MCP;
using LLMDesktopAssistant.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace LLMDesktopAssistant.Avalonia.LLM.Settings
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
			get => _settingsVm.Parent.Settings.UsedMcpServers.Contains(_server.Id);
			set
			{
				if (value)
				{
					if (!_settingsVm.Parent.Settings.UsedMcpServers.Contains(_server.Id))
					{
						_settingsVm.Parent.Settings.UsedMcpServers.Add(_server.Id);
						_settingsVm.EnsureMCPServers();
					}
				}
				else
				{
					if (_settingsVm.Parent.Settings.UsedMcpServers.Remove(_server.Id))
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
		public ChatSettingsViewModel Parent { get; }
		public ChatSettings Settings { get; }

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

		public ChatMCPSettingsViewModel(ChatSettingsViewModel parent)
		{
			Parent = parent;
			Settings = parent.Settings;

			var mcpConfig = MCPManager.GetConfiguration();
			McpServers = mcpConfig.Servers
				.Select(s => new MCPServerSelectionViewModel(s, this))
				.ToImmutableList();

			EnsureMCPServers();
		}

		private bool _ensuringMcp = false;
		public async void EnsureMCPServers()
		{
			var managerService = Parent.Chat.Services.GetRequiredService<IMCPManagementService>();

			if (_ensuringMcp)
				return;
			_ensuringMcp = true;

			try
			{
				await managerService.EnsureCurrentMCPConnectionsAsync();
				Parent.ToolSettings.UpdateTools();
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