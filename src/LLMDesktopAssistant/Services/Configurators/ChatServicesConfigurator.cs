using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Services.Configurators
{
	[ServiceConfigurator(ServiceScope.Chat)]
	public class ChatServicesConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			var chatServices = ReflectionUtility.GetTypesWithAttributes<ChatServiceAttribute>().ToList();
			foreach (var service in chatServices)
			{
				foreach (var attribute in service.Attributes)
				{
					services.AddScoped(attribute.ServiceType ?? service.Type, service.Type);
				}
			}
		}
	}
}