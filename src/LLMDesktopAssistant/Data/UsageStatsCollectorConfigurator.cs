using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Data
{
	[ServiceConfigurator(ServiceScope.App)]
	public class UsageStatsCollectorConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			var usageDatabase = new UsageDatabase(Path.Combine(Directories.Data, "usage.db"));
			services.AddSingleton(usageDatabase);
		}
	}
}