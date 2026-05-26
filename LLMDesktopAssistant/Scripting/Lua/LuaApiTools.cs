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
			  Calls a registered tool by name and returns a structured result table.

			  Parameters:
			    - name: string — tool name (e.g. "web-search", "fs-read_entry")
			    - args: table — arguments to pass to the tool, converted to JSON internally.
			      Use string keys matching the tool's argument schema.
			      Booleans: true / false
			      Arrays: { "a", "b", "c" }
			      Nested objects: { key = { subkey = "value" } }
			      If the tool requires no arguments, pass an empty table {}.

			  Returns: table — structured result with the following fields:
			    - content: string - the textual result produced by the tool
				- structured: table or nil - the optional structured result produced by the tool
			    - success: boolean — whether the tool executed successfully
			    - tool: table — the table containing info of the tool that was called (see dass.tools.list() for what that table contains)
			    - status_title: string or nil — optional status title
			    - status_icon: string or nil — optional status icon name

			  Throws an error if the tool name is not found or execution fails.
			  Use pcall() for safe error handling.

			--- dass.tools.list()
			  Returns a table with detailed information about all registered tools.

			  Returns: table — array of tool info tables. Each entry contains:
			    - name: string — tool name (e.g. "web-search")
			    - description: string — tool description
			    - category: string — tool category (e.g. "web", "scripting")
			    - display_name: string or nil — user-friendly name
			    - enabled: boolean — whether the tool is enabled
			    - ask_for_confirmation: boolean — whether user confirmation is required
			    - source: string — tool source ("native", "meta", "mcp")
			    - arguments: table — JSON schema of the arguments

			EXAMPLES:

			  -- Flip a coin (structured result)
			  local r = dass.tools.call("random-coin_flip", {})
			  print("Success:", r.success)
			  print("Result:", r.content)

			  -- Search the web
			  local r = dass.tools.call("web-search", {
			    query = "weather in London",
			    maxResults = 3
			  })
			  if r.success then
			    print(r.content)
			  end

			  -- Read a file
			  local r = dass.tools.call("fs-read_entry", {
			    path = "README.md"
			  })
			  print(r.content)

			NOTES:
			- Lua tables are automatically serialized to JSON.
			- Table keys must be strings (quotes optional in Lua,
			  but use ["key-name"] for keys containing spaces or hyphens).
			- The result is a structured table containing content, success status, and metadata.
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
			ns["list"] = DynValue.NewCallback(ListTools);
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
			var reactiveResult = tool.Executor.Invoke(jsonArgs, context, CancellationToken.None).Result;

			var script = ctx.GetScript();

			// Wait for completion to get success status
			var success = reactiveResult.Completion.GetAwaiter().GetResult();
			var content = reactiveResult.ResultContent;
			var structured = JsonLuaConverter.JsonNodeToDynValue(script, reactiveResult.StructuredResult);

			// Build structured result table
			var resultTable = new Table(script);
			resultTable["content"] = content;
			resultTable["structured"] = structured;
			resultTable["success"] = DynValue.NewBoolean(success);
			resultTable["tool"] = ToolToTable(tool, script);
			if (reactiveResult.StatusTitle != null)
				resultTable["status_title"] = reactiveResult.StatusTitle;
			if (reactiveResult.StatusIcon != null)
				resultTable["status_icon"] = reactiveResult.StatusIcon.ToString();

			return DynValue.NewTable(resultTable);
		}

		public DynValue ListTools(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var script = ctx.GetScript();
			var resultTable = new Table(script);

			int i = 1;
			foreach (var (_, tool) in _toolsetCache.AvailableTools)
			{
				var entry = ToolToTable(tool, script);
				resultTable.Set(i++, DynValue.NewTable(entry));
			}

			return DynValue.NewTable(resultTable);
		}

		private static Table ToolToTable(ToolInfo tool, Script script)
		{
			var result = new Table(script);
			result["name"] = tool.Name;
			result["description"] = tool.DescriptionGetter();
			result["category"] = tool.Category;
			if (tool.DisplayName != null)
				result["display_name"] = tool.DisplayName;
			result["enabled"] = DynValue.NewBoolean(tool.Enabled);
			result["ask_for_confirmation"] = DynValue.NewBoolean(tool.AskForConfirmation);
			result["source"] = tool.Source.ToString().ToLower();
			result["arguments"] = JsonLuaConverter.JsonNodeToDynValue(script, tool.ArgumentSchema);
			return result;
		}
	}
}

