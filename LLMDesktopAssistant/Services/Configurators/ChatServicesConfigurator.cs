using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Services.Configurators
{
	[ServiceConfigurator(ServiceScope.Chat)]
	public class ChatServicesConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			var chatServices = ReflectionUtility.GetTypesWithAttribute<ChatServiceAttribute>().ToList();
			foreach (var service in chatServices)
			{
				services.AddScoped(service.Attribute.ServiceType ?? service.Type, service.Type);
			}
		}
	}
}