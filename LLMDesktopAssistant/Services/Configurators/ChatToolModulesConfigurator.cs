using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Services.Configurators
{
	[ServiceConfigurator(ServiceScope.Chat)]
	public class ChatToolModulesConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			var toolModules = ReflectionUtility.GetTypesWithAttribute<ToolModule, ToolModuleAttribute>();
			foreach (var toolModule in toolModules)
			{
				if (toolModule.Attribute.ChatScoped)
					services.AddScoped(typeof(ToolModule), toolModule.Type);
			}
		}
	}
}