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
			services.AddScoped<IAttachmentApplicationService, AttachmentApplicationService>();

			var chatServices = ReflectionUtility.GetTypesWithAttribute<ChatServiceAttribute>().ToList();
			foreach (var service in chatServices)
			{
				services.AddScoped(service.Attribute.ServiceType ?? service.Type, service.Type);
			}

			var toolModules = ReflectionUtility.GetTypesWithAttribute<ToolModule, ToolModuleAttribute>().ToList();
			foreach (var toolModule in toolModules)
			{
				services.AddScoped(typeof(ToolModule), toolModule.Type);
			}

			var luaApis = ReflectionUtility.GetTypesWithAttribute<LuaApiBase, LuaApiAttribute>().ToList();
			foreach (var luaApi in luaApis)
			{
				services.AddScoped(typeof(LuaApiBase), luaApi.Type);
			}

			foreach (var configurator in ReflectionUtility.GetTypesWithAttribute<ServiceConfigurator, ServiceConfiguratorAttribute>())
			{
				if (configurator.Attribute.Scope == ServiceScope.Chat)
					configurator.Type.Instantiate<ServiceConfigurator>().Configure(services);
			}
		}
	}
}