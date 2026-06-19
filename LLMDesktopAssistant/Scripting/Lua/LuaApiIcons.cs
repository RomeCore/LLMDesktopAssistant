using Material.Icons;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for Material Design icons: <c>icon.*</c>.
	/// Provides access to MaterialIconKind enum for status icons, toasts, etc.
	/// </summary>
	[LuaApi(chatScoped: false)]
	public class LuaApiIcons : LuaApiBase
	{
		public override string? Namespace => "icon";

		public override string? Manuals => """
			--- icon — Material Design icons API

			Provides access to Material Design icons (MaterialIconKind enum).
			Useful for setting status icons in tool results, toasts, etc.

			FUNCTIONS:

			--- icon.exists(kind)
			  Checks if an icon with the given name exists.
			  Parameters:
			    - kind: string — icon name (case-insensitive, e.g. "File", "Web", "Alert")
			  Returns: boolean

			--- icon.get(kind)
			  Gets detailed information about an icon by its name.
			  Parameters:
			    - kind: string — icon name (case-insensitive)
			  Returns: table or nil — with fields:
			    - name: string — canonical icon name
			    - kind: number — numeric enum value

			--- icon.list([filter])
			  Lists all available icon names.
			  Parameters:
			    - filter: string (optional) — substring filter (case-insensitive)
			  Returns: table — array of icon name strings (sorted alphabetically)

			--- icon.random()
			  Returns a random icon name.
			  Returns: string — random icon name

			EXAMPLES:

			  if icon.exists("Alert") then
			    print("Alert icon exists!")
			  end

			  local info = icon.get("File")
			  print(info.name, info.kind)

			  -- Filter icons
			  local fileIcons = icon.list("file")
			  for _, name in ipairs(fileIcons) do
			    print(name)
			  end

			  -- Use with tool result
			  dass.tool_result.set_status(icon.get("Download").name, "Downloading...")

			NOTES:
			  - All lookups are case-insensitive.
			  - The list is cached at startup for performance.
			  - There are over 7000 icons available.
			""";

		private static readonly Dictionary<string, MaterialIconKind> _iconMap;
		private static readonly string[] _allIconNames;

		static LuaApiIcons()
		{
			_iconMap = Enum.GetValues<MaterialIconKind>()
				.DistinctBy(k => k.ToString().ToLowerInvariant())
				.ToDictionary(k => k.ToString(), StringComparer.OrdinalIgnoreCase);
			_allIconNames = _iconMap.Keys.ToArray();

			Array.Sort(_allIconNames, StringComparer.OrdinalIgnoreCase);
		}

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["exists"] = DynValue.NewCallback(Exists);
			ns["get"] = DynValue.NewCallback(Get);
			ns["list"] = DynValue.NewCallback(List);
			ns["random"] = DynValue.NewCallback(Random);
		}

		private DynValue Exists(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var kind = args[0].CastToString();
			if (string.IsNullOrEmpty(kind))
				return DynValue.False;

			return DynValue.NewBoolean(_iconMap.ContainsKey(kind));
		}

		private DynValue Get(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var kind = args[0].CastToString();
			if (string.IsNullOrEmpty(kind))
				return DynValue.Nil;

			if (!_iconMap.TryGetValue(kind, out var iconKind))
				return DynValue.Nil;

			var script = ctx.GetScript();
			var table = new Table(script);
			table["name"] = iconKind.ToString();
			table["kind"] = (int)iconKind;
			return DynValue.NewTable(table);
		}

		private DynValue List(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var script = ctx.GetScript();
			var result = new Table(script);

			if (args.Count == 0)
			{
				foreach (var name in _allIconNames)
				{
					result.Append(DynValue.NewString(name));
				}
			}
			else
			{
				var filter = args[0].CastToString();
				foreach (var name in _allIconNames)
				{
					if (name.Contains(filter, StringComparison.OrdinalIgnoreCase))
					{
						result.Append(DynValue.NewString(name));
					}
				}
			}

			return DynValue.NewTable(result);
		}

		private DynValue Random(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var index = System.Random.Shared.Next(_allIconNames.Length);
			return DynValue.NewString(_allIconNames[index]);
		}
	}
}
