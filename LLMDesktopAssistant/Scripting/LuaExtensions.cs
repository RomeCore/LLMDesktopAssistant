using LLMDesktopAssistant.Tools;
using MoonSharp.Interpreter;
using Serilog;

namespace LLMDesktopAssistant.Scripting
{
	public static class LuaExtensions
	{
		public static Table ShallowClone(this Table table)
		{
			if (table == null)
				return null!;

			var newTable = new Table(table.OwnerScript);

			foreach (var kv in table.Pairs)
			{
				if (kv.Key.String == LuaVariables.GlobalTable)
					continue;
				newTable.Set(kv.Key, kv.Value);
			}
			newTable.MetaTable = table.MetaTable.DeepClone(table.OwnerScript);

			return newTable;
		}

		public static Table DeepClone(this Table table, Script? script = null)
		{
			if (table == null)
				return null!;

			script ??= table.OwnerScript;
			var newTable = new Table(script);

			foreach (var kv in table.Pairs)
			{
				if (kv.Key.String == LuaVariables.GlobalTable)
					continue;
				if (kv.Value.Type == DataType.Table)
					newTable.Set(kv.Key, DynValue.NewTable(DeepClone(kv.Value.Table, script)));
				else
					newTable.Set(kv.Key, kv.Value);
			}
			newTable.MetaTable = table.MetaTable?.DeepClone(script);

			return newTable;
		}

		/// <summary>
		/// Creates a deep clone of this table merged with another table (non-mutating).
		/// The original tables are not modified - a new table is always returned.
		/// For keys that exist in both tables:
		///   1) If both values are tables, they are merged recursively into a new table.
		///   2) Otherwise, the value from <paramref name="otherTable"/> is used.
		/// For keys only in <paramref name="otherTable"/>, they are added.
		/// Keys only in <paramref name="table"/> are preserved (deep-cloned).
		/// All nested tables are deep-cloned to avoid reference sharing.
		/// </summary>
		/// <param name="table">The source table (not modified).</param>
		/// <param name="otherTable">The table to merge in (not modified).</param>
		/// <param name="script">Optional script for creating new tables. Defaults to <paramref name="table"/>.OwnerScript.</param>
		/// <returns>A new table that is the merged result of both input tables.</returns>
		public static Table DeepMergeWith(this Table table, Table otherTable, Script? script = null)
		{
			if (table == null)
				return otherTable;
			if (otherTable == null)
				return table;

			script ??= table.OwnerScript;

			var result = DeepClone(table, script);
			foreach (var kv in otherTable.Pairs)
			{
				if (kv.Key.String == LuaVariables.GlobalTable)
					continue;

				var existingValue = result.Get(kv.Key);

				if (existingValue.Type == DataType.Table && kv.Value.Type == DataType.Table)
				{
					result.Set(kv.Key, DynValue.NewTable(existingValue.Table.DeepMergeWith(kv.Value.Table, script)));
				}
				else
				{
					if (kv.Value.Type == DataType.Table)
						result.Set(kv.Key, DynValue.NewTable(kv.Value.Table.DeepClone(script)));
					else
						result.Set(kv.Key, kv.Value);
				}
			}

			return result;
		}

		/// <summary>
		/// Creates a snapshot of the current Lua state, including all global variables and tables.
		/// Useful for concurrent execution.
		/// </summary>
		/// <param name="script">The Lua script to take a snapshot of.</param>
		/// <returns>A new Lua script that is a snapshot of the original.</returns>
		public static Script CreateSnapshot(this Script script)
		{
			var snapshotLua = new Script(CoreModules.None);

			// Options
			snapshotLua.Options.Stderr = script.Options.Stderr;
			snapshotLua.Options.Stdout = script.Options.Stdout;
			snapshotLua.Options.DebugPrint = script.Options.DebugPrint;
			snapshotLua.Options.ColonOperatorClrCallbackBehaviour = script.Options.ColonOperatorClrCallbackBehaviour;
			snapshotLua.Options.ScriptLoader = script.Options.ScriptLoader;
			snapshotLua.Options.CheckThreadAccess = script.Options.CheckThreadAccess;
			snapshotLua.Options.TailCallOptimizationThreshold = script.Options.TailCallOptimizationThreshold;
			snapshotLua.Options.UseLuaErrorLocations = script.Options.UseLuaErrorLocations;

			// Globals
			var originalGlobals = script.Globals;
			var snapshotGlobals = snapshotLua.Globals;
			foreach (var kvp in originalGlobals.Pairs)
			{
				DynValue value = kvp.Value;
				if (value.Type == DataType.Table)
					value = DynValue.NewTable(value.Table.DeepClone(snapshotLua));
				snapshotGlobals.Set(kvp.Key, value);
			}
			snapshotGlobals.Set(LuaVariables.GlobalTable, DynValue.NewTable(snapshotGlobals));
			snapshotGlobals.MetaTable = originalGlobals.MetaTable.DeepClone(snapshotLua);

			// Type metatables
			foreach (var dataType in LuaConstants.SupportedTypeMetatables)
			{
				try
				{
					snapshotLua.SetTypeMetatable(dataType, script.GetTypeMetatable(dataType)?.DeepClone(snapshotLua));
				}
				catch (Exception ex)
				{
					Log.Debug("Failed to clone type metatable for {DataType} in LuaExtensions.CreateSnapshot(): {Error}", dataType, ex);
				}
			}

			return snapshotLua;
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
