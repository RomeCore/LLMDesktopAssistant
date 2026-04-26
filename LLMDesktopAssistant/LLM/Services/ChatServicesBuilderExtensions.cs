using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Prompting;
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

			foreach (var toolModule in ReflectionUtility.GetTypesWithAttribute<ToolModule, ToolModuleAttribute>())
			{
				services.AddScoped(typeof(ToolModule), toolModule.Type);
			}

			foreach (var service in ReflectionUtility.GetTypesWithAttribute<ChatServiceAttribute>())
			{
				services.AddScoped(service.Attribute.ServiceType ?? service.Type, service.Type);
			}
		}
	}
}