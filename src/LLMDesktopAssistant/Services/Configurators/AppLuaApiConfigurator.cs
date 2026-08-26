using LLMDesktopAssistant.Scripting.Lua;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Services.Configurators
{
	[ServiceConfigurator(ServiceScope.App)]
	public class AppLuaApiConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			var luaApis = ReflectionUtility.GetTypesWithAttribute<LuaApiBaseAsync, LuaApiAttribute>().ToList();
			foreach (var luaApi in luaApis)
			{
				if (!luaApi.Attribute.ChatScoped)
					services.AddSingleton(typeof(LuaApiBaseAsync), luaApi.Type);
			}
		}
	}
}