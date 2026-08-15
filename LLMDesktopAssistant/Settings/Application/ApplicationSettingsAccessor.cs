namespace LLMDesktopAssistant.Settings.Application
{
	public static class ApplicationSettingsAccessor
	{
		private static ApplicationSettings? _applicationSettings;
		/// <summary>
		/// Gets the application settings. If they have not been set yet, they will be retrieved from the settings manager.
		/// </summary>
		public static ApplicationSettings ApplicationSettings => _applicationSettings ??= SettingsManager.Get<ApplicationSettings>();

		/// <summary>
		/// Sets the application settings. This is useful for testing purposes.
		/// </summary>
		/// <param name="applicationSettings">The application settings to set. Use null to reset to default.</param>
		public static void SetApplicationSettings(ApplicationSettings? applicationSettings)
		{
			_applicationSettings = applicationSettings;
		}
	}
}
