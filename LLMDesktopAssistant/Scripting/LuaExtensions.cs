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

		public static ToolExecutionContext? TryGetToolExecutionContext(this ScriptExecutionContext ctx)
		{
			var globals = ctx.CurrentGlobalEnv ?? ctx.OwnerScript.Globals;
			var tecDv = globals.Get("_dass_tec");
			if (tecDv.Type == DataType.UserData)
				return tecDv.ToObject() as ToolExecutionContext;
			return null;
		}
	}
}
