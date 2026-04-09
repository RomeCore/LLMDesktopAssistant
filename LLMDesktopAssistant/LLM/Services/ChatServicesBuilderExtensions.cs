using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.ToolModules;
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

namespace LLMDesktopAssistant.LLM.Services
{
	public static class ChatServicesBuilderExtensions
	{
		public static void AddChatServices(this IServiceCollection services)
		{
			services.TryAddSingleton(Log.Logger);

			services.TryAddSingleton(services =>
			{
				var templateLibrary = new TemplateLibrary();
				templateLibrary.SetLanguageFallbackScheme(new MajorLanguageFallbackScheme());
				templateLibrary.ImportFromAssembly(typeof(ChatServicesBuilderExtensions).Assembly);
				return templateLibrary;
			});
			services.TryAddSingleton<IChatManagementService, ChatManagementService>();
			services.TryAddSingleton<IDocumentReadingService, DocumentReadingService>();
			services.TryAddSingleton<IMessageTokenSerializationSchema>(MessageTokenSerializationSchema.Default);
			services.TryAddSingleton<IUsageStatsCollector, UsageStatsCollector>();

			services.AddScoped<Chat>();
			services.TryAddScoped<IMCPManagementService, MCPManagementService>();
			services.TryAddScoped<IChatOperationService, ChatOperationService>();
			services.TryAddScoped<IChatExecutionService, ChatExecutionService>();
			services.TryAddScoped<IChatStorageService, ChatStorageService>();
			services.TryAddScoped<IChatSummarizationService, ChatSummarizationService>();
			services.TryAddScoped<IPromptChatBuilder, PromptChatBuilder>();
			services.TryAddScoped<IToolExecutionService, ToolExecutionService>();
			services.TryAddScoped<IToolsetBuildingService, ToolsetBuildingService>();
			services.TryAddScoped<ILLMBuildingService, LLMBuildingService>();
			services.TryAddScoped<IAttachmentApplicationService, AttachmentApplicationService>();
			services.TryAddScoped<IMetaToolManagementService, MetaToolManagementService>();

			services.AddScoped<ToolModule, PythonInterpreterToolModule>();
			services.AddScoped<ToolModule, ShellInterpreterToolModule>();
			services.AddScoped<ToolModule, FilesystemToolModule>();
			services.AddScoped<ToolModule, WebRequestToolModule>();
			services.AddScoped<ToolModule, AgenticToolModule>();
			services.AddScoped<ToolModule, MetaToolModule>();
		}
	}
}