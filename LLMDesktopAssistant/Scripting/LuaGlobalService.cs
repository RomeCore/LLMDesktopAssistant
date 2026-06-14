using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting
{
	[Service]
	public class LuaGlobalService
	{
		public LuaGlobalService()
		{
			foreach (var asm in ReflectionUtility.ObservedAssemblies)
				UserData.RegisterAssembly(asm);
		}
	}
}