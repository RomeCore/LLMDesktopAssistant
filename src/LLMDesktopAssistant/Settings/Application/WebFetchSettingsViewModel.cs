using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils.Web;

namespace LLMDesktopAssistant.Settings.Application
{
	/// <summary>
	/// ViewModel for the web fetching settings category: browser defaults and
	/// manual installation of the Playwright and stealth CloakBrowser runtimes.
	/// </summary>
	[ViewModelFor(typeof(WebFetchSettingsView))]
	public class WebFetchSettingsViewModel : ViewModelBase
	{
		private readonly WebFetchSettings _settings;
		private readonly IWebBrowserInstaller _installer;

		private bool _isInstallingPlaywright;
		private bool _isInstallingCloakBrowser;

		/// <summary>
		/// Gets or sets a value indicating whether pages should be loaded with
		/// a headless browser by default.
		/// </summary>
		public bool UseBrowser
		{
			get => _settings.UseBrowser;
			set => _settings.UseBrowser = value;
		}

		/// <summary>
		/// Gets or sets a value indicating whether pages should be loaded with
		/// the stealth CloakBrowser by default.
		/// </summary>
		public bool UseStealthBrowser
		{
			get => _settings.UseStealthBrowser;
			set => _settings.UseStealthBrowser = value;
		}

		/// <summary>
		/// Gets a value indicating whether the Playwright Chromium runtime is installed.
		/// </summary>
		public bool IsPlaywrightInstalled => _installer.IsPlaywrightInstalled;

		/// <summary>
		/// Gets a value indicating whether the stealth CloakBrowser runtime is installed.
		/// </summary>
		public bool IsCloakBrowserInstalled => _installer.IsCloakBrowserInstalled;

		/// <summary>
		/// Gets the localized status text for the Playwright runtime.
		/// </summary>
		public string PlaywrightStatusText => LocalizationManager.LocalizeStatic(
			IsPlaywrightInstalled ? "settings.web.installed" : "settings.web.not_installed");

		/// <summary>
		/// Gets the localized status text for the stealth CloakBrowser runtime.
		/// </summary>
		public string CloakBrowserStatusText => LocalizationManager.LocalizeStatic(
			IsCloakBrowserInstalled ? "settings.web.installed" : "settings.web.not_installed");

		/// <summary>
		/// Gets a value indicating whether the Playwright runtime can be installed now.
		/// </summary>
		public bool CanInstallPlaywright => !IsInstallingPlaywright && !IsPlaywrightInstalled;

		/// <summary>
		/// Gets a value indicating whether the stealth CloakBrowser can be installed now.
		/// </summary>
		public bool CanInstallCloakBrowser => !IsInstallingCloakBrowser && !IsCloakBrowserInstalled;

		/// <summary>
		/// Gets a value indicating whether a Playwright installation is in progress.
		/// </summary>
		public bool IsInstallingPlaywright
		{
			get => _isInstallingPlaywright;
			private set
			{
				if (SetProperty(ref _isInstallingPlaywright, value))
					RaisePropertyChanged(nameof(CanInstallPlaywright));
			}
		}

		/// <summary>
		/// Gets a value indicating whether a CloakBrowser installation is in progress.
		/// </summary>
		public bool IsInstallingCloakBrowser
		{
			get => _isInstallingCloakBrowser;
			private set
			{
				if (SetProperty(ref _isInstallingCloakBrowser, value))
					RaisePropertyChanged(nameof(CanInstallCloakBrowser));
			}
		}

		/// <summary>
		/// Gets the command that installs the Playwright Chromium runtime.
		/// </summary>
		public IAsyncRelayCommand InstallPlaywrightCommand { get; }

		/// <summary>
		/// Gets the command that installs the stealth CloakBrowser runtime.
		/// </summary>
		public IAsyncRelayCommand InstallCloakBrowserCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="WebFetchSettingsViewModel"/> class.
		/// </summary>
		public WebFetchSettingsViewModel()
		{
			_settings = ApplicationSettingsAccessor.ApplicationSettings.WebFetch;
			_installer = ServiceRegistry.Provider.GetRequiredService<IWebBrowserInstaller>();

			InstallPlaywrightCommand = new AsyncRelayCommand(
				async () =>
				{
					IsInstallingPlaywright = true;
					try
					{
						await _installer.InstallPlaywrightAsync();
					}
					finally
					{
						IsInstallingPlaywright = false;
						RaisePropertyChanged(nameof(IsPlaywrightInstalled));
						RaisePropertyChanged(nameof(PlaywrightStatusText));
						RaisePropertyChanged(nameof(CanInstallPlaywright));
					}
				},
				() => CanInstallPlaywright);

			InstallCloakBrowserCommand = new AsyncRelayCommand(
				async () =>
				{
					IsInstallingCloakBrowser = true;
					try
					{
						await _installer.InstallCloakBrowserAsync();
					}
					finally
					{
						IsInstallingCloakBrowser = false;
						RaisePropertyChanged(nameof(IsCloakBrowserInstalled));
						RaisePropertyChanged(nameof(CloakBrowserStatusText));
						RaisePropertyChanged(nameof(CanInstallCloakBrowser));
					}
				},
				() => CanInstallCloakBrowser);
		}
	}
}
