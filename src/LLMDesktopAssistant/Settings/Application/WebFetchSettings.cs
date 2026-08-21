using LLMDesktopAssistant.Utils.Web;

namespace LLMDesktopAssistant.Settings.Application
{
	/// <summary>
	/// Web fetching settings category of the application.
	/// </summary>
	public class WebFetchSettings : ApplicationSettingsCategoryBase
	{
		private bool _enableBrowser = true;
		/// <summary>
		/// Gets or sets a value indicating whether the headless browser
		/// <see cref="WebFetchLevel"/> is allowed for page loads.
		/// </summary>
		public bool EnableBrowser
		{
			get => _enableBrowser;
			set => SetProperty(ref _enableBrowser, value);
		}

		private bool _enableStealthBrowser = true;
		/// <summary>
		/// Gets or sets a value indicating whether the stealth CloakBrowser
		/// <see cref="WebFetchLevel"/> is allowed for page loads.
		/// </summary>
		public bool EnableStealthBrowser
		{
			get => _enableStealthBrowser;
			set => SetProperty(ref _enableStealthBrowser, value);
		}

		private WebFetchLevel _defaultFetchLevel = WebFetchLevel.HttpClient;
		/// <summary>
		/// Gets or sets the default <see cref="WebFetchLevel"/> used for page
		/// loads when the caller does not request a higher level.
		/// </summary>
		public WebFetchLevel DefaultFetchLevel
		{
			get => _defaultFetchLevel;
			set => SetProperty(ref _defaultFetchLevel, value);
		}
	}
}
