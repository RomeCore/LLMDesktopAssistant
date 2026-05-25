using LLMDesktopAssistant.Tools;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting
{
	public static class LuaExtensions
	{
		public static Table ShallowClone(this Table table)
		{
			var newTable = new Table(table.OwnerScript);

			foreach (var kv in table.Pairs)
			{
				newTable.Set(kv.Key, kv.Value);
			}

			return newTable;
		}

		public static Table DeepClone(this Table table)
		{
			var newTable = new Table(table.OwnerScript);

			foreach (var kv in table.Pairs)
			{
				if (kv.Value.Type == DataType.Table)
					newTable.Set(kv.Key, DynValue.NewTable(DeepClone(kv.Value.Table)));
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
