using LLMDesktopAssistant.Desktop.Utils.Web;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LLMDesktopAssistant.Desktop.Services.Configurators
{
	/// <summary>
	/// Replaces the default <see cref="IWebFetcher"/> with the WebReaper-backed
	/// <see cref="WebReaperWebFetcher"/>, which adds headless-browser and stealth
	/// CloakBrowser support, wrapped in an <see cref="AutoEscalatingWebFetcher"/>
	/// that escalates the fetch level automatically when a page denies access.
	/// </summary>
	[ServiceConfigurator(ServiceScope.App)]
	public class DesktopWebFetcherConfigurator : ServiceConfigurator
	{
		/// <inheritdoc />
		public override void Configure(IServiceCollection services)
		{
			foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IWebFetcher)).ToArray())
				services.Remove(descriptor);

			services.AddSingleton<WebReaperWebFetcher>();
			services.AddSingleton<IWebFetcher>(sp => new AutoEscalatingWebFetcher(
				sp.GetRequiredService<WebReaperWebFetcher>(),
				sp.GetRequiredService<ILogger>()));

			if (!services.Any(d => d.ServiceType == typeof(IWebBrowserInstaller)))
				services.AddSingleton<IWebBrowserInstaller, WebReaperWebBrowserInstaller>();
		}
	}
}
