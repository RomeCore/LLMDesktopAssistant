using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.MCP;
using Serilog;
using System.Collections.ObjectModel;

namespace LLMDesktopAssistant.MCP
{
	public class MCPServerItemViewModel : ViewModelBase
	{
		public MCPServerInfo Info { get; }
		public IReadOnlyList<MCPConnectionType> ConnectionTypes { get; } = [MCPConnectionType.Stdio, MCPConnectionType.Remote];

		public MCPServerItemViewModel(MCPServerInfo info)
		{
			Info = info;
		}

		private bool _isConnected;
		public bool IsConnected
		{
			get => _isConnected;
			set => SetProperty(ref _isConnected, value);
		}

		private bool _isBusy;
		public bool IsBusy
		{
			get => _isBusy;
			set => SetProperty(ref _isBusy, value);
		}

		public IAsyncRelayCommand ConnectCommand => new AsyncRelayCommand(Connect);
		public IAsyncRelayCommand DisconnectCommand => new AsyncRelayCommand(Disconnect);

		private async Task Connect()
		{
			if (IsBusy)
				return;

			try
			{
				IsBusy = true;

				await MCPManager.EnsureConnectionAsync(Info.Id);
				IsConnected = true;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to connect to MCP server: {Name}", Info.Name);
				IsConnected = false;
			}
			finally
			{
				IsBusy = false;
			}
		}

		private async Task Disconnect()
		{
			if (IsBusy)
				return;

			try
			{
				IsBusy = true;

				await MCPManager.DisconnectAsync(Info.Id);
				IsConnected = false;
			}
			finally
			{
				IsBusy = false;
			}
		}
	}

	[ViewModelFor(typeof(MCPManagerView))]
	public class MCPManagerViewModel : ViewModelBase
	{
		public MCPConfiguration Configuration { get; }
		public ObservableCollection<MCPServerItemViewModel> Servers { get; }

		private MCPServerItemViewModel? _selectedServer;
		public MCPServerItemViewModel? SelectedServer
		{
			get => _selectedServer;
			set
			{
				if (SetProperty(ref _selectedServer, value))
					RemoveCommand.NotifyCanExecuteChanged();
			}
		}

		public RelayCommand AddCommand { get; }
		public RelayCommand RemoveCommand { get; }

		public MCPManagerViewModel()
		{
			Configuration = MCPManager.GetConfiguration();
			Servers = new ObservableCollection<MCPServerItemViewModel>(
				Configuration.Servers.Select(s => new MCPServerItemViewModel(s))
			);

			AddCommand = new RelayCommand(Add);
			RemoveCommand = new RelayCommand(Remove, () => SelectedServer != null);

			foreach (var vm in Servers)
			{
				vm.IsConnected = MCPManager.CheckConnection(vm.Info.Id, out _);
			}
		}

		private void Add()
		{
			var model = new MCPServerInfo
			{
				Name = "New Server",
				ConnectionType = MCPConnectionType.Stdio
			};

			Configuration.Servers.Add(model);

			var vm = new MCPServerItemViewModel(model);
			Servers.Add(vm);
			SelectedServer = vm;
		}

		private void Remove()
		{
			if (SelectedServer == null)
				return;

			Configuration.Servers.Remove(SelectedServer.Info);
			Servers.Remove(SelectedServer);
			SelectedServer = null;
		}
	}
}