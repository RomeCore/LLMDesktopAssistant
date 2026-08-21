namespace LLMDesktopAssistant.Utils.Web
{
	/// <summary>
	/// Installs and detects the browser runtimes used by
	/// <see cref="IWebFetcher"/> implementations: the Playwright Chromium
	/// runtime and the stealth CloakBrowser.
	/// </summary>
	public interface IWebBrowserInstaller
	{
		/// <summary>
		/// Gets a value indicating whether the Playwright Chromium runtime is installed.
		/// </summary>
		bool IsPlaywrightInstalled { get; }

		/// <summary>
		/// Gets a value indicating whether the stealth CloakBrowser runtime is installed.
		/// </summary>
		bool IsCloakBrowserInstalled { get; }

		/// <summary>
		/// Installs the Playwright Chromium runtime.
		/// </summary>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task InstallPlaywrightAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Installs the stealth CloakBrowser runtime.
		/// </summary>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task InstallCloakBrowserAsync(CancellationToken cancellationToken = default);
	}
}
