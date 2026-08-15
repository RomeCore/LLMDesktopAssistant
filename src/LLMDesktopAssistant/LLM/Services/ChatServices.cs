using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
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

			var serviceBuilder = new ServiceCollection();
			serviceBuilder.AddKeyedSingleton<IServiceCollection>(ServiceKeys.ChatServices, serviceBuilder);
			serviceBuilder.AddAppServices();
			serviceBuilder.AddSingleton(database);
			serviceBuilder.AddChatServices();
			serviceBuilder.DeduplicateServices();
			ServiceProvider = serviceBuilder.BuildServiceProvider();

			ManagementService = ServiceProvider.GetRequiredService<IChatManagementService>();

			Log.Information("ChatServices initialized with {Count} Chat services.", serviceBuilder.Count);
		}
	}
}