using AsyncLua;
using LLMDesktopAssistant.Tools;
using Serilog;

namespace LLMDesktopAssistant.Scripting
{
	public static class LuaExtensions
	{
		// Bruh, the most of extensions gone to AsyncLua

		public static T? TryGetUserData<T>(this LuaCallingContext ctx, string variableName)
			where T : class
		{
			var globals = ctx.Globals;
			return globals.Get<T>(variableName);
		}

		public static ToolExecutionContext? TryGetToolExecutionContext(this LuaCallingContext ctx)
		{
			return TryGetUserData<ToolExecutionContext>(ctx, LuaVariables.ToolExecutionContext);
		}

		public static ReactiveToolResult? TryGetReactiveToolResult(this LuaCallingContext ctx)
		{
			return TryGetUserData<ReactiveToolResult>(ctx, LuaVariables.ToolReactiveResult);
		}
	}
}
