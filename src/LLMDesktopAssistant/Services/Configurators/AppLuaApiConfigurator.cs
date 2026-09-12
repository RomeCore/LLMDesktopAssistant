using LLMDesktopAssistant.Scripting.Lua;
using LLMDesktopAssistant.Scripting.Lua.API;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Services.Configurators
{
	[ServiceConfigurator(ServiceScope.App)]
	public class AppLuaApiConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			var luaApis = ReflectionUtility.GetTypesWithAttribute<LuaApiBase, LuaApiAttribute>().ToList();
			foreach (var luaApi in luaApis)
			{
				if (!luaApi.Attribute.ChatScoped)
					services.AddSingleton(typeof(LuaApiBase), luaApi.Type);
			}
		}
	}
}