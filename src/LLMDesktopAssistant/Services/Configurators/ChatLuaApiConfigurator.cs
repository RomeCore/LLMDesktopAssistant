using LLMDesktopAssistant.Scripting.Lua;
using LLMDesktopAssistant.Scripting.Lua.API;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Services.Configurators
{
	[ServiceConfigurator(ServiceScope.Chat)]
	public class ChatLuaApiConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			var luaApis = ReflectionUtility.GetTypesWithAttribute<LuaApiBase, LuaApiAttribute>().ToList();
			foreach (var luaApi in luaApis)
			{
				if (luaApi.Attribute.ChatScoped)
					services.AddScoped(typeof(LuaApiBase), luaApi.Type);
			}
		}
	}
}