using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils.Web;

namespace LLMDesktopAssistant.Services.Configurators
{
	/// <summary>
	/// Registers the default <see cref="IWebFetcher"/> implementation. A more
	/// capable implementation registered earlier (for example, a WebReaper-backed
	/// fetcher in the desktop app) is left untouched.
	/// </summary>
	[ServiceConfigurator(ServiceScope.App)]
	public class WebFetcherConfigurator : ServiceConfigurator
	{
		/// <inheritdoc />
		public override void Configure(IServiceCollection services)
		{
			if (!services.Any(d => d.ServiceType == typeof(IWebFetcher)))
				services.AddSingleton<IWebFetcher, HttpClientWebFetcher>();

			if (!services.Any(d => d.ServiceType == typeof(IWebBrowserInstaller)))
				services.AddSingleton<IWebBrowserInstaller, NoopWebBrowserInstaller>();
		}
	}
}
