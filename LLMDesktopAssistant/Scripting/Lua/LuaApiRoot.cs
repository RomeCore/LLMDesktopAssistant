using System.Reflection;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API root namespace <c>dass.*</c>.
	/// Provides application-level utilities: version, help, working directory.
	/// </summary>
	[LuaApi(chatScoped: true)]
	public class LuaApiRoot : LuaApiBase
	{
		private readonly FileAccessService _fileAccess;
		private static readonly string _version = App.Version.ToString();

		public override string? Namespace => "dass";

		public override string? Manuals => $"""
			--- dass — application-level utilities

			Provides version info, help system, and working directory.

			FUNCTIONS:

			--- dass.version()
			  Returns the application version string.
			  Returns: string — e.g. "1.0.0"

			--- dass.help()
			  Prints the manuals for all available Lua API namespaces.
			  Returns: nil (output goes to Lua console via print)

			--- dass.working_dir()
			  Returns the current chat's working directory (absolute path).
			  Returns: string — e.g. "{Directories.DefaultWorkingDirectory}"

			EXAMPLES:

			  print(dass.version())        -- "1.0.0"
			  dass.help()                  -- prints all available APIs
			""";

		public LuaApiRoot(FileAccessService fileAccess)
		{
			_fileAccess = fileAccess;
		}

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["version"] = DynValue.NewCallback(new CallbackFunction((ctx, args) =>
				DynValue.NewString(_version)));

			ns["help"] = DynValue.NewCallback(new CallbackFunction((ctx, args) =>
			{
				var script = ctx.GetScript();
				var printFunc = globals.Get("print");

				foreach (var nsName in luaService.Namespaces)
				{
					var displayName = nsName ?? "_G (global)";
					var nsTable = nsName != null ? luaService.TryResolveNamespace(nsName) : globals;
					if (nsTable == null) continue;

					var manuals = nsTable.Get("_manuals");
					if (manuals.Type != DataType.Table) continue;

					// Print header
					if (printFunc.Type == DataType.Function)
						script.Call(printFunc, DynValue.NewString($"\n=== {displayName} ==="));

					foreach (var m in manuals.Table.Values)
					{
						if (m.Type == DataType.String && printFunc.Type == DataType.Function)
							script.Call(printFunc, m);
					}
				}

				return DynValue.Nil;
			}));

			ns["working_dir"] = DynValue.NewCallback(new CallbackFunction((ctx, args) =>
				DynValue.NewString(_fileAccess.GetWorkingDirectory())));
		}
	}
}
