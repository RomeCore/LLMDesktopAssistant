using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace LLMDesktopAssistant.LLM.Services
{
	public static class ChatServices
	{
		public static IServiceProvider ServiceProvider { get; }
		public static IChatManagementService ManagementService { get; }

		static ChatServices()
		{
			var database = new ChatDatabase(Path.Combine(Directories.Data, "chat.db"));
			var usageDatabase = new UsageDatabase(Path.Combine(Directories.Data, "usage.db"));

			var serviceBuilder = new ServiceCollection();
			serviceBuilder.AddKeyedSingleton<IServiceCollection>(ServiceRegistry.ChatServicesKey, serviceBuilder);
			serviceBuilder.AddAppServices();
			serviceBuilder.AddSingleton(database);
			serviceBuilder.AddSingleton(usageDatabase);
			serviceBuilder.AddChatServices();
			ServiceProvider = serviceBuilder.BuildServiceProvider();

			ManagementService = ServiceProvider.GetRequiredService<IChatManagementService>();

			Log.Information("ChatServices initialized with {Count} Chat services.", serviceBuilder.Count);
		}
	}
}