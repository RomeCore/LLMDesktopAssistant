using System.Text.Json;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Tools;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Serialization.Json;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[LuaApi]
	public class LuaApiTools : LuaApiBase
	{
		public override string? Namespace => "dass.tools";

		public override string? Manuals => """
			--- dass.tools — assistant's tool system

			Provides access to all registered tools (filesystem, web search, Python
			execution, etc.) directly from Lua scripts.

			FUNCTIONS:

			--- dass.tools.call(name, args)
			  Calls a registered tool by name and returns its string output.

			  Parameters:
			    - name: string — tool name (e.g. "web-search", "fs-read_entry")
			    - args: table — arguments to pass to the tool, converted to JSON internally.
			      Use string keys matching the tool's argument schema.
			      Booleans: true / false
			      Arrays: { "a", "b", "c" }
			      Nested objects: { key = { subkey = "value" } }
			      If the tool requires no arguments, pass an empty table {}.

			  Returns: string — the textual result produced by the tool.

			  Throws an error if the tool name is not found or execution fails.
			  Use pcall() for safe error handling.

			--- dass.tools.list()
			  Returns a table with names of all tools registered currently (available and unavailable).
			  (Planned for future implementation)

			EXAMPLES:

			  -- Flip a coin
			  local coin = dass.tools.call("random-coin_flip", {})
			  print("Result: " .. coin)

			  -- Search the web
			  local results = dass.tools.call("web-search", {
			    query = "weather in London",
			    maxResults = 3
			  })

			  -- Read a file
			  local content = dass.tools.call("fs-read_entry", {
			    path = "README.md"
			  })

			  -- Random chance check
			  local chance = dass.tools.call("random-check_chance", {
			    chance = 75
			  })

			NOTES:
			- Lua tables are automatically serialized to JSON.
			- Table keys must be strings (quotes optional in Lua,
			  but use ["key-name"] for keys containing spaces or hyphens).
			- The result is always returned as a plain string.
			- Always wrap calls in pcall() when the tool may fail.
			""";

		private readonly IToolsetCacheService _toolsetCache;
		private readonly IServiceProvider _services;

		public LuaApiTools(IToolsetCacheService toolsetCache, IServiceProvider services)
		{
			_toolsetCache = toolsetCache;
			_services = services;
		}

		public override void Populate(Table globals, Table ns)
		{
			ns["call"] = DynValue.NewCallback(Call);
		}

		public DynValue Call(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var function = args[0].CastToString();
			var arguments = args[1];
			if (arguments.Type != DataType.Table)
				arguments = DynValue.NewTable(ctx.GetScript());

			if (!_toolsetCache.AvailableTools.TryGetValue(function, out var tool))
				throw new ScriptRuntimeException($"Tool '{function}' not found.");

			var jsonArgs = JsonLuaConverter.DynValueToJsonNode(arguments) ?? new JsonObject();
			var context = ctx.TryGetToolExecutionContext();
			if (context == null)
			{
				var chat = _services.GetRequiredService<Chat>();
				context = ToolExecutionContext.CreateDummy(tool, jsonArgs, chat);
			}
			var result = tool.Executor.Invoke(jsonArgs, context, CancellationToken.None).Result;
			return DynValue.NewString(result.ResultContent);
		}
	}
}
