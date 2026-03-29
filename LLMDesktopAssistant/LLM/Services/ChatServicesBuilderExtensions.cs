using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.LLM.Domain;
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

			services.TryAddScoped<Chat>();
			services.TryAddScoped<IChatOperationService, ChatOperationService>();
			services.TryAddScoped<IChatExecutionService, ChatExecutionService>();
			services.TryAddScoped<IChatStorageService, ChatStorageService>();
			services.TryAddScoped<IPromptChatBuilder, PromptChatBuilder>();
			services.TryAddScoped<IToolExecutionService, ToolExecutionService>();
			services.TryAddScoped<ILLMProvider, LLMProvider>();
		}
	}
}