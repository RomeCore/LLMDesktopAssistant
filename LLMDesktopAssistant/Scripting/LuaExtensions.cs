using AsyncLua;
using AsyncLua.Values;
using LLMDesktopAssistant.Tools;
using Serilog;

namespace LLMDesktopAssistant.Scripting
{
	public static class LuaExtensions
	{
		/// <summary>
		/// Determines whether a Lua table should be serialized as a JSON array.
		/// A table is considered an array if it has a contiguous sequence of
		/// integer keys from 1 to <see cref="Table.Length"/> and no keys
		/// outside that range.
		/// </summary>
		public static bool IsArrayTable(this LuaTable table)
		{
			int len = table.Length;

			if (len == 0 && table.Keys.Count() == 0)
				return true;

			if (len == 0)
				return false;

			for (int i = 1; i <= len; i++)
			{
				if (!table.Keys.Contains(new LuaNumber(i)))
					return false;
			}

			foreach (var key in table.Keys)
			{
				if (key is not LuaNumber numberKey)
					return false;
				double num = numberKey.Value;
				if (num < 1 || num > len || num != Math.Truncate(num))
					return false;
			}

			return true;
		}

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
