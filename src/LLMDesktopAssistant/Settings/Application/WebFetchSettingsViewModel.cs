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
		/// Gets or sets a value indicating whether the headless browser
		/// <see cref="WebFetchLevel"/> is allowed for page loads.
		/// </summary>
		public bool EnableBrowser
		{
			get => _settings.EnableBrowser;
			set => _settings.EnableBrowser = value;
		}

		/// <summary>
		/// Gets or sets a value indicating whether the stealth CloakBrowser
		/// <see cref="WebFetchLevel"/> is allowed for page loads.
		/// </summary>
		public bool EnableStealthBrowser
		{
			get => _settings.EnableStealthBrowser;
			set => _settings.EnableStealthBrowser = value;
		}

		/// <summary>
		/// Gets the available <see cref="WebFetchLevel"/> options for the
		/// default fetch level selector.
		/// </summary>
		public IReadOnlyList<FetchLevelItemViewModel> FetchLevels { get; } = Enum.GetValues<WebFetchLevel>()
			.Select(level => new FetchLevelItemViewModel(level, LocalizationManager.LocalizeStatic(LevelKey(level))))
			.ToArray();

		private static string LevelKey(WebFetchLevel level) => level switch
		{
			WebFetchLevel.HttpClient => "settings.web.level.http",
			WebFetchLevel.Browser => "settings.web.level.browser",
			WebFetchLevel.StealthBrowser => "settings.web.level.stealth",
			_ => throw new ArgumentOutOfRangeException(nameof(level))
		};

		/// <summary>
		/// Gets or sets the selected default <see cref="WebFetchLevel"/> item.
		/// </summary>
		public FetchLevelItemViewModel? SelectedFetchLevel
		{
			get => FetchLevels.FirstOrDefault(item => item.Value == _settings.DefaultFetchLevel);
			set
			{
				if (value != null)
					_settings.DefaultFetchLevel = value.Value;
			}
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

	/// <summary>
	/// A selectable <see cref="WebFetchLevel"/> option with a localized display name.
	/// </summary>
	public sealed class FetchLevelItemViewModel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FetchLevelItemViewModel"/> class.
		/// </summary>
		/// <param name="value">The fetch level value.</param>
		/// <param name="displayName">The localized display name.</param>
		public FetchLevelItemViewModel(WebFetchLevel value, string displayName)
		{
			Value = value;
			DisplayName = displayName;
		}

		/// <summary>
		/// Gets the fetch level value.
		/// </summary>
		public WebFetchLevel Value { get; }

		/// <summary>
		/// Gets the localized display name.
		/// </summary>
		public string DisplayName { get; }
	}
}
