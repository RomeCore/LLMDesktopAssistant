namespace LLMDesktopAssistant.Utils.Web
{
	/// <summary>
	/// The default <see cref="IWebBrowserInstaller"/> used on platforms that
	/// do not reference WebReaper: browser runtimes are never installed and
	/// install requests fail with <see cref="NotSupportedException"/>.
	/// </summary>
	public sealed class NoopWebBrowserInstaller : IWebBrowserInstaller
	{
		/// <inheritdoc />
		public bool IsPlaywrightInstalled => false;

		/// <inheritdoc />
		public bool IsCloakBrowserInstalled => false;

		/// <inheritdoc />
		public Task InstallPlaywrightAsync(CancellationToken cancellationToken = default)
			=> throw new NotSupportedException("Playwright installation is not supported on this platform.");

		/// <inheritdoc />
		public Task InstallCloakBrowserAsync(CancellationToken cancellationToken = default)
			=> throw new NotSupportedException("CloakBrowser installation is not supported on this platform.");
	}
}
