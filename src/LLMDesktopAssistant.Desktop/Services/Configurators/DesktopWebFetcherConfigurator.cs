using LLMDesktopAssistant.Desktop.Utils.Web;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils.Web;
using Microsoft.Extensions.DependencyInjection;

namespace LLMDesktopAssistant.Desktop.Services.Configurators
{
	/// <summary>
	/// Replaces the default <see cref="IWebFetcher"/> with the WebReaper-backed
	/// <see cref="WebReaperWebFetcher"/>, which adds headless-browser support
	/// (<see cref="WebFetchOptions.UseBrowser"/>).
	/// </summary>
	[ServiceConfigurator(ServiceScope.App)]
	public class DesktopWebFetcherConfigurator : ServiceConfigurator
	{
		/// <inheritdoc />
		public override void Configure(IServiceCollection services)
		{
			foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IWebFetcher)).ToArray())
				services.Remove(descriptor);
			services.AddSingleton<IWebFetcher, WebReaperWebFetcher>();

			if (!services.Any(d => d.ServiceType == typeof(IWebBrowserInstaller)))
				services.AddSingleton<IWebBrowserInstaller, WebReaperWebBrowserInstaller>();
		}
	}
}
