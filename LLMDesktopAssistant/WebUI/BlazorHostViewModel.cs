using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.WebUI
{
	[ViewModelFor(typeof(BlazorHostView))]
	public class BlazorHostViewModel : ViewModelBase
	{
		private readonly IChatWebUIStarter? _blazorStarter;
		private readonly IPasswordHashingService _passwordHashingService;
		private readonly WebUIStartupSettings _settings;

		public WebUIStartupSettings Settings => _settings;

		private bool _isRunning;
		public bool IsRunning
		{
			get => _isRunning;
			private set => SetProperty(ref _isRunning, value);
		}

		public IAsyncRelayCommand StartCommand { get; }
		public IAsyncRelayCommand StopCommand { get; }
		public ICommand CopyUrlCommand { get; }
		public ICommand OpenInBrowserCommand { get; }

		public bool OpenedInDialog { get; }
		public ICommand CloseDialogCommand { get; }

		public BlazorHostViewModel(IServiceProvider chatServices)
		{
			_blazorStarter = chatServices.GetService<IChatWebUIStarter>();
			_passwordHashingService = chatServices.GetRequiredService<IPasswordHashingService>();
			_settings = SettingsManager.Get<WebUIStartupSettings>();

			if (_blazorStarter != null)
				_blazorStarter.PropertyChanged += OnStarterPropertyChanged;

			StartCommand = new AsyncRelayCommand(StartAsync, () => _blazorStarter != null && !_blazorStarter.IsRunning);
			StopCommand = new AsyncRelayCommand(StopAsync, () => _blazorStarter != null && _blazorStarter.IsRunning);
			CopyUrlCommand = new RelayCommand(CopyUrl);
			OpenInBrowserCommand = new RelayCommand(OpenInBrowser);

			OpenedInDialog = true;
			CloseDialogCommand = new RelayCommand(() =>
			{
				DialogManager.CloseDialog();
			});

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
			StartCommand.NotifyCanExecuteChanged();
			StopCommand.NotifyCanExecuteChanged();
		}

		private async Task StartAsync()
		{
			if (_blazorStarter == null)
				return;

			try
			{
				if (!string.IsNullOrEmpty(_settings.Password))
					_settings.PasswordHash = _passwordHashingService.HashPassword(_settings.Password);
				else
					_settings.PasswordHash = null;

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
				App.MainTopLevel.Clipboard?.SetTextAsync(Settings.EndpointUrl);
			}
			catch { }
		}

		private void OpenInBrowser()
		{
			try
			{
				Process.Start(new ProcessStartInfo(Settings.EndpointUrl) { UseShellExecute = true });
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
