using LLMDesktopAssistant.Scripting.Lua;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Services.Configurators
{
	[ServiceConfigurator(ServiceScope.Chat)]
	public class LuaApiConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			var luaApis = ReflectionUtility.GetTypesWithAttribute<LuaApiBaseAsync, LuaApiAttribute>().ToList();
			foreach (var luaApi in luaApis)
			{
				services.AddScoped(typeof(LuaApiBaseAsync), luaApi.Type);
			}
		}
	}
}