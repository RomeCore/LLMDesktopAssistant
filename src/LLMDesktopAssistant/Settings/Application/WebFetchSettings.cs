namespace LLMDesktopAssistant.Settings.Application
{
	/// <summary>
	/// Web fetching settings category of the application.
	/// </summary>
	public class WebFetchSettings : ApplicationSettingsCategoryBase
	{
		private bool _useBrowser;
		/// <summary>
		/// Gets or sets a value indicating whether pages should be loaded with
		/// a headless browser (rendering JavaScript) by default.
		/// </summary>
		public bool UseBrowser
		{
			get => _useBrowser;
			set => SetProperty(ref _useBrowser, value);
		}

		private bool _useStealthBrowser;
		/// <summary>
		/// Gets or sets a value indicating whether pages should be loaded with
		/// the stealth CloakBrowser by default.
		/// </summary>
		public bool UseStealthBrowser
		{
			get => _useStealthBrowser;
			set => SetProperty(ref _useStealthBrowser, value);
		}
	}
}
