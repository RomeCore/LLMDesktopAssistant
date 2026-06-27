using System.Text;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/*

	[LuaApi(chatScoped: false)]
	public class LuaApiManuals : LuaApiBase
	{
		public override string? Namespace => null;

		public override string? Manuals => """
			--- manuals(namespace) — global function

			Gets the documentation (manuals) for a given Lua API namespace.
			Use this to explore what functions and features are available.

			Parameters:
			  - namespace (one or more): string or table — dot-separated namespace path,
			    e.g. dass, dass.tool, dass.chat, fs, time

			Returns: string — the manuals text for the requested namespace(s).

			Throws an error if the namespace does not exist
			or has no manuals registered.

			Examples:
			  print(manuals(dass))
			  print(manuals(dass.tool, dass.tool.result, fs, time))
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
			var script = ctx.GetScript();

			var result = new StringBuilder();

			for (int i = 0; i < args.Count; i++)
			{
				var nsArg = args[i];
				Table? nsTable;

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
					nsTable = script.Globals;
				}

				// Look for a "_manuals" subtable inside the resolved namespace
				var manuals = nsTable.Get(LuaVariables.NamespaceManuals);
				var nsPath = nsTable.Get(LuaVariables.NamespaceFullPath).ToPrintString();

				if (manuals.Type != DataType.Table || manuals.Table.Length == 0)
				{
					result.AppendLine($"No manuals found for namespace '{nsPath}'.");
				}
				else
				{
					result.AppendLine($"--- Manuals for namespace '{nsPath}' ---");
					result.AppendLine();

					foreach (var entry in manuals.Table.Values)
					{
						if (entry.Type == DataType.String)
							result.AppendLine(entry.String.TrimEnd()).AppendLine();
					}
				}
			}

			return DynValue.NewString(result.ToString().TrimEnd());
		}
	}

	*/
}
