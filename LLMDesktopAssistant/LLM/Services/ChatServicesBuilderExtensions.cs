using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.ToolModules;
using LLTSharp;
using LLTSharp.Locale;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

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

			services.AddScoped<Chat>();
			services.TryAddScoped<IMCPManagementService, MCPManagementService>();
			services.TryAddScoped<IChatOperationService, ChatOperationService>();
			services.TryAddScoped<IChatExecutionService, ChatExecutionService>();
			services.TryAddScoped<IChatStorageService, ChatStorageService>();
			services.TryAddScoped<IPromptChatBuilder, PromptChatBuilder>();
			services.TryAddScoped<IToolExecutionService, ToolExecutionService>();
			services.TryAddScoped<IToolsetBuildingService, ToolsetBuildingService>();
			services.TryAddScoped<ILLMBuildingService, LLMBuildingService>();

			services.AddScoped<ToolModule, PythonInterpreterToolModule>();
			services.AddScoped<ToolModule, ShellInterpreterToolModule>();
			services.AddScoped<ToolModule, FilesystemToolModule>();
			services.AddScoped<ToolModule, WebRequestToolModule>();
		}
	}
}