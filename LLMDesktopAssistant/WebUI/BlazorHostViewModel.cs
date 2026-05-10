using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Settings;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace LLMDesktopAssistant.WebUI
{
	[ViewModelFor(typeof(BlazorHostView))]
	public class BlazorHostViewModel : ViewModelBase
	{
		private readonly IChatWebUIStarter? _blazorStarter;
		private readonly WebUIStartupSettings _settings;

		public BlazorHostViewModel(IServiceProvider chatServices)
		{
			_blazorStarter = chatServices.GetService<IChatWebUIStarter>();
			_settings = SettingsManager.Get<WebUIStartupSettings>();

			if (_blazorStarter != null)
				_blazorStarter.PropertyChanged += OnStarterPropertyChanged;

			StartCommand = new AsyncRelayCommand(StartAsync, () => _blazorStarter != null && !_blazorStarter.IsRunning);
			StopCommand = new AsyncRelayCommand(StopAsync, () => _blazorStarter != null && _blazorStarter.IsRunning);
			CopyUrlCommand = new RelayCommand(CopyUrl);
			OpenInBrowserCommand = new RelayCommand(OpenInBrowser);

			UpdateStatus();
		}

		private void OnStarterPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IChatWebUIStarter.IsRunning))
			{
				InvokeUI(() => UpdateStatus());
			}
		}

		private void UpdateStatus()
		{
			IsRunning = _blazorStarter?.IsRunning ?? false;
			EndpointUrl = _settings.EndpointUrl;
			StartCommand.NotifyCanExecuteChanged();
			StopCommand.NotifyCanExecuteChanged();
		}

		private bool _isRunning;
		public bool IsRunning
		{
			get => _isRunning;
			private set => SetProperty(ref _isRunning, value);
		}

		private string _endpointUrl = "http://localhost:5000";
		public string EndpointUrl
		{
			get => _endpointUrl;
			private set => SetProperty(ref _endpointUrl, value);
		}

		private string? _password;
		public string? Password
		{
			get => _password;
			set => SetProperty(ref _password, value);
		}

		public IAsyncRelayCommand StartCommand { get; }
		public IAsyncRelayCommand StopCommand { get; }
		public ICommand CopyUrlCommand { get; }
		public ICommand OpenInBrowserCommand { get; }

		private async Task StartAsync()
		{
			if (_blazorStarter == null)
				return;

			try
			{
				_settings.EndpointUrl = EndpointUrl;

				if (!string.IsNullOrWhiteSpace(Password))
					_settings.PasswordHash = Password;

				_blazorStarter.Start(_settings);
				UpdateStatus();
			}
			catch (Exception ex)
			{
				Serilog.Log.Error(ex, "Failed to start Blazor host");
			}
		}

		private async Task StopAsync()
		{
			if (_blazorStarter == null)
				return;

			try
			{
				_blazorStarter.Stop();
				UpdateStatus();
			}
			catch (Exception ex)
			{
				Serilog.Log.Error(ex, "Failed to stop Blazor host");
			}
		}

		private void CopyUrl()
		{
			try
			{
				App.MainTopLevel.Clipboard?.SetTextAsync(EndpointUrl);
			}
			catch { }
		}

		private void OpenInBrowser()
		{
			try
			{
				Process.Start(new ProcessStartInfo(EndpointUrl) { UseShellExecute = true });
			}
			catch { }
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing && _blazorStarter != null)
				_blazorStarter.PropertyChanged -= OnStarterPropertyChanged;
		}
	}
}
