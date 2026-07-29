using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels.Statistics;
using Serilog;

namespace LLMDesktopAssistant.LLM.Services
{
	public static class ChatServicesBuilderExtensions
	{
		public static void AddChatServices(this IServiceCollection services)
		{
			services.AddSingleton(Log.Logger);

			services.AddSingleton(sp => sp.GetRequiredService<IPromptRegistry>().SharedLibrary);
			services.AddSingleton<IChatManagementService, ChatManagementService>();
			services.AddSingleton<IDocumentReadingService, DocumentReadingService>();
			services.AddSingleton<IMessageTokenSerializationSchema>(MessageTokenSerializationSchema.Default);
			services.AddSingleton<IUsageStatsCollector, UsageStatsCollector>();

			services.AddScoped<Chat>();

			foreach (var configurator in ReflectionUtility.GetTypesWithAttribute<ServiceConfigurator, ServiceConfiguratorAttribute>())
			{
				if (configurator.Attribute.Scope == ServiceScope.Chat)
					configurator.Type.Instantiate<ServiceConfigurator>().Configure(services);
			}
		}
	}
}