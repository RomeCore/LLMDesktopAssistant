using LLMDesktopAssistant.Tools;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting
{
	public static class LuaExtensions
	{
		public static Table ShallowClone(this Table table, Script? script = null)
		{
			script ??= table.OwnerScript;
			var newTable = new Table(script);

			foreach (var kv in table.Pairs)
			{
				if (kv.Key.String == "_G")
					continue;
				newTable.Set(kv.Key, kv.Value);
			}

			return newTable;
		}

		public static Table DeepClone(this Table table, Script? script = null)
		{
			script ??= table.OwnerScript;
			var newTable = new Table(script);

			foreach (var kv in table.Pairs)
			{
				if (kv.Key.String == "_G")
					continue;
				if (kv.Value.Type == DataType.Table)
					newTable.Set(kv.Key, DynValue.NewTable(DeepClone(kv.Value.Table, script)));
				else
					newTable.Set(kv.Key, kv.Value);
			}

			return newTable;
		}

		public static T? TryGetUserData<T>(this ScriptExecutionContext ctx, string variableName)
			where T : class
		{
			var globals = ctx.CurrentGlobalEnv ?? ctx.OwnerScript.Globals;
			var userData = globals.Get(variableName);
			if (userData.Type == DataType.UserData)
				return userData.ToObject() as T;
			return null;
		}

		public static ToolExecutionContext? TryGetToolExecutionContext(this ScriptExecutionContext ctx)
		{
			return TryGetUserData<ToolExecutionContext>(ctx, LuaVariables.ToolExecutionContext);
		}

		public static ReactiveToolResult? TryGetReactiveToolResult(this ScriptExecutionContext ctx)
		{
			return TryGetUserData<ReactiveToolResult>(ctx, LuaVariables.ToolReactiveResult);
		}
	}
}
