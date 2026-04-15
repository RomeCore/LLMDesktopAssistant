using LLMDesktopAssistant.Core.LLM.Data;
using LLMDesktopAssistant.Core.Services;
using LLMDesktopAssistant.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.LLM.Services
{
	public static class ChatServices
	{
		public static IServiceProvider ServiceProvider { get; }
		public static IChatManagementService ManagementService { get; }

		static ChatServices()
		{
			var database = new ConversationDatabase(Path.Combine(Directories.Data, "chat.db"));
			var usageDatabase = new UsageDatabase(Path.Combine(Directories.Data, "usage.db"));

			var serviceBuilder = new ServiceCollection();
			serviceBuilder.AddAppServices();
			serviceBuilder.AddSingleton(database);
			serviceBuilder.AddSingleton(usageDatabase);
			serviceBuilder.AddChatServices();
			ServiceProvider = serviceBuilder.BuildServiceProvider();

			ManagementService = ServiceProvider.GetRequiredService<IChatManagementService>();
		}
	}
}