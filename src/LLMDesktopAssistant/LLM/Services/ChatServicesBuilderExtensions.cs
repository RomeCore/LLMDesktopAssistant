using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Services
{
	public static class ChatServicesBuilderExtensions
	{
		public static void AddChatServices(this IServiceCollection services)
		{
			services.AddSingleton<IChatManagementService, ChatManagementService>();
			services.AddScoped<Chat>();

			foreach (var configurator in ReflectionUtility.GetTypesWithAttribute<ServiceConfigurator, ServiceConfiguratorAttribute>())
			{
				if (configurator.Attribute.Scope == ServiceScope.Chat)
					configurator.Type.Instantiate<ServiceConfigurator>().Configure(services);
			}
		}
	}
}