using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Blazor.Services
{
	[ServiceConfigurator(ServiceScope.WebUI)]
	public class WebUIServicesConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			var webUIServices = ReflectionUtility.GetTypesWithAttribute<WebUIServiceAttribute>().ToList();
			foreach (var service in webUIServices)
			{
				if (service.Attribute.IsScoped)
					services.AddScoped(service.Attribute.ServiceType ?? service.Type, service.Type);
				else
					services.AddSingleton(service.Attribute.ServiceType ?? service.Type, service.Type);
			}
		}
	}
}
