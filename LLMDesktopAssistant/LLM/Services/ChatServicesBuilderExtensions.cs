using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Scripting.Lua;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using LLTSharp;
using LLTSharp.Locale;
using Microsoft.Extensions.DependencyInjection;
using RCLargeLanguageModels.Statistics;
using Serilog;

namespace LLMDesktopAssistant.LLM.Services
{
	public static class ChatServicesBuilderExtensions
	{
		public static void AddChatServices(this IServiceCollection services)
		{
			services.AddSingleton(Log.Logger);

			services.AddSingleton(PromptRegistry.SharedLibrary);
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