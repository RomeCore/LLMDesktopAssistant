using LLMDesktopAssistant.Core.LLM.Data;
using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.LLM.Services.Attachments;
using LLMDesktopAssistant.Core.LLM.Services.Tools;
using LLMDesktopAssistant.Core.ToolModules;
using LLMDesktopAssistant.Core.ToolModules.Implementations;
using LLMDesktopAssistant.Core.Utils;
using LLTSharp;
using LLTSharp.Locale;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCLargeLanguageModels.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.LLM.Services
{
	public static class ChatServicesBuilderExtensions
	{
		public static void AddChatServices(this IServiceCollection services)
		{
			services.AddSingleton(Log.Logger);

			services.AddSingleton(services =>
			{
				var templateLibrary = new TemplateLibrary();
				templateLibrary.SetLanguageFallbackScheme(new MajorLanguageFallbackScheme());
				templateLibrary.ImportFromAssembly(typeof(ChatServicesBuilderExtensions).Assembly);
				return templateLibrary;
			});
			services.AddSingleton<IChatManagementService, ChatManagementService>();
			services.AddSingleton<IDocumentReadingService, DocumentReadingService>();
			services.AddSingleton<IMessageTokenSerializationSchema>(MessageTokenSerializationSchema.Default);
			services.AddSingleton<IUsageStatsCollector, UsageStatsCollector>();

			services.AddScoped<Chat>();
			services.AddScoped<IMCPManagementService, MCPManagementService>();
			services.AddScoped<IChatOperationService, ChatOperationService>();
			services.AddScoped<IChatExecutionService, ChatExecutionService>();
			services.AddScoped<IChatStorageService, ChatStorageService>();
			services.AddScoped<IChatSummarizationService, ChatSummarizationService>();
			services.AddScoped<IPromptChatBuilder, PromptChatBuilder>();
			services.AddScoped<IToolExecutionService, ToolExecutionService>();
			services.AddScoped<IToolsetBuildingService, ToolsetBuildingService>();
			services.AddScoped<ILLMBuildingService, LLMBuildingService>();
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