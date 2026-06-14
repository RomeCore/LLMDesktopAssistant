using System.Text;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[LuaApi]
	public class LuaApiManuals : LuaApiBase
	{
		public override string? Namespace => null;

		public override string? Manuals => """
			--- manuals(namespace) — global function

			Gets the documentation (manuals) for a given Lua API namespace.
			Use this to explore what functions and features are available.

			Parameters:
			  - namespace: string or table — dot-separated namespace path,
			    e.g. dass, dass.tools, dass.chat

			Returns: string — the manuals text for the requested namespace.

			Throws an error if the namespace does not exist
			or has no manuals registered.

			Examples:
			  print(manuals(dass))
			  print(manuals(dass.tools))
			""";

		private readonly IServiceProvider _services;

		public LuaApiManuals(IServiceProvider services)
		{
			_services = services;
		}

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			globals["manuals"] = DynValue.NewCallback(PrintManuals);
		}

		private DynValue PrintManuals(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var lua = _services.GetRequiredService<LuaService>();

			var nsArg = args[0];
			Table? nsTable = null;

			if (nsArg.Type == DataType.String)
			{
				nsTable = lua.TryResolveNamespace(nsArg.String);
				if (nsTable == null)
					return DynValue.NewString($"Error: namespace '{nsArg.String}' not found.");
			}
			else if (nsArg.Type == DataType.Table)
			{
				nsTable = nsArg.Table;
			}
			else
			{
				nsTable = lua.Lua.Globals;
			}

			// Look for a "_manuals" subtable inside the resolved namespace
			var manuals = nsTable.Get("_manuals");
			var nsPath = nsTable.Get("_ns_path").ToPrintString();
			if (manuals.Type != DataType.Table || manuals.Table.Length == 0)
				return DynValue.NewString($"No manuals found for namespace '{nsPath}'.");

			var result = new StringBuilder();
			result.AppendLine($"--- Manuals for namespace '{nsPath}' ---");
			result.AppendLine();

			foreach (var entry in manuals.Table.Values)
			{
				if (entry.Type == DataType.String)
					result.AppendLine(entry.String.TrimEnd()).AppendLine();
			}

			return DynValue.NewString(result.ToString().TrimEnd());
		}
	}
}
