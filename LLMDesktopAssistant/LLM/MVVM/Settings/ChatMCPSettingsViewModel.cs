using System.ComponentModel;
using Avalonia.Collections;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.MCP;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Represents a single MCP server with an enabled toggle routed through the effective
	/// (inherited) used servers list of the chat.
	/// </summary>
	public class MCPServerSelectionViewModel : ViewModelBase
	{
		private readonly MCPServerInfo _server;
		private readonly ChatMCPSettingsViewModel _settingsVm;

		/// <summary>
		/// Gets the name of the MCP server.
		/// </summary>
		public string Name => _server.Name;

		/// <summary>
		/// Gets the endpoint of the MCP server.
		/// </summary>
		public string Endpoint => _server.Endpoint;

		/// <summary>
		/// Gets the connection type of the MCP server.
		/// </summary>
		public MCPConnectionType ConnectionType => _server.ConnectionType;

		/// <summary>
		/// Initializes a new instance of the <see cref="MCPServerSelectionViewModel"/> class.
		/// </summary>
		/// <param name="server">The MCP server info.</param>
		/// <param name="settingsVm">The parent settings view model.</param>
		public MCPServerSelectionViewModel(MCPServerInfo server, ChatMCPSettingsViewModel settingsVm)
		{
			_server = server;
			_settingsVm = settingsVm;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this MCP server is used by the chat.
		/// </summary>
		public bool IsEnabled
		{
			get => _settingsVm.EffectiveUsedMcpServers.Contains(_server.Id);
			set
			{
				if (value)
				{
					if (!_settingsVm.EffectiveUsedMcpServers.Contains(_server.Id))
					{
						_settingsVm.EffectiveUsedMcpServers.Add(_server.Id);
						_settingsVm.EnsureMCPServers();
					}
				}
				else
				{
					if (_settingsVm.EffectiveUsedMcpServers.Remove(_server.Id))
					{
						_settingsVm.EnsureMCPServers();
					}
				}

				RaisePropertyChanged(nameof(IsEnabled));
			}
		}

		/// <summary>
		/// Re-evaluates and raises the <see cref="IsEnabled"/> change notification.
		/// Called when the inheritance level changes and the effective used servers list is swapped.
		/// </summary>
		public void Refresh()
		{
			RaisePropertyChanged(nameof(IsEnabled));
		}
	}

	/// <summary>
	/// ViewModel for the MCP settings tab.
	/// The used servers list is resolved through the effective (inherited) scope, selected via
	/// the inheritance level combo box in the view.
	/// </summary>
	[ViewModelFor(typeof(ChatMCPSettingsView))]
	public class ChatMCPSettingsViewModel : ViewModelBase
	{
		private readonly IMCPManagementService _mcpManagementService;

		/// <summary>
		/// Gets the underlying MCP settings.
		/// </summary>
		public ChatMcpSettings McpSettings { get; }

		/// <summary>
		/// Gets the effective used MCP server Ids resolved by the current inheritance level.
		/// </summary>
		public ICollection<Guid> EffectiveUsedMcpServers => McpSettings.GetEffectiveUsedMcpServers();

		private InheritanceLevelItem _selectedUsedMcpServersInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the used MCP servers group.
		/// </summary>
		public InheritanceLevelItem SelectedUsedMcpServersInheritance
		{
			get => _selectedUsedMcpServersInheritance;
			set
			{
				if (SetProperty(ref _selectedUsedMcpServersInheritance, value) && value != null)
					McpSettings.UsedMcpServersInheritance = value.Value;
			}
		}

		private AvaloniaList<MCPServerSelectionViewModel> _mcpServers = [];
		/// <summary>
		/// Gets or sets the list of MCP servers with per-chat enabled toggles.
		/// </summary>
		public ICollection<MCPServerSelectionViewModel> McpServers
		{
			get => _mcpServers;
			set
			{
				_mcpServers.Clear();
				_mcpServers.AddRange(value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatMCPSettingsViewModel"/> class.
		/// </summary>
		/// <param name="settings">The MCP settings to edit.</param>
		/// <param name="mcpManagementService">The MCP management service used to ensure connections.</param>
		public ChatMCPSettingsViewModel(ChatMcpSettings settings, IMCPManagementService mcpManagementService)
		{
			_mcpManagementService = mcpManagementService;
			McpSettings = settings;

			_selectedUsedMcpServersInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.UsedMcpServersInheritance);
			settings.PropertyChanged += McpSettings_PropertyChanged;

			var mcpConfig = MCPManager.GetConfiguration();
			McpServers = mcpConfig.Servers
				.Select(s => new MCPServerSelectionViewModel(s, this))
				.ToImmutableList();

			EnsureMCPServers();
		}

		private void McpSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// The generated UsedMcpServersInheritance setter raises PropertyChanged with the
			// name of the inherited property ("UsedMcpServers") when the level changes.
			if (e.PropertyName != "UsedMcpServers")
				return;

			_selectedUsedMcpServersInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == McpSettings.UsedMcpServersInheritance);
			RaisePropertyChanged(nameof(SelectedUsedMcpServersInheritance));
			RaisePropertyChanged(nameof(EffectiveUsedMcpServers));

			foreach (var server in _mcpServers)
				server.Refresh();

			EnsureMCPServers();
		}

		private bool _ensuringMcp = false;

		/// <summary>
		/// Ensures that the MCP connections for the current effective servers list are established.
		/// </summary>
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
