using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AsyncLua;
using AsyncLua.Values;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[LuaApi(chatScoped: true)]
	public class LuaApiTools : LuaApiBaseAsync
	{
		public override string? Namespace => "dass.tool";

		public override string? Manuals => """
			--- dass.tool — assistant's tool system

			Provides access to all registered tools (filesystem, web search, Python
			execution, etc.) directly from Lua scripts.

			FUNCTIONS:

			--- async dass.tool.call(name, args)
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
			    - tool: table — the table containing info of the tool that was called (see dass.tool.list() for what that table contains)
			    - status_title: string or nil — optional status title
			    - status_icon: string or nil — optional status icon name

			  Throws an error if the tool name is not found or execution fails.
			  Use pcall() for safe error handling.

			--- dass.tool.list()
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
			  local r = await dass.tool.call("random-coin_flip", {})
			  print("Success:", r.success)
			  print("Result:", r.content)

			  -- Search the web
			  local r = await dass.tool.call("web-search", {
			    query = "weather in London",
			    maxResults = 3
			  })
			  if r.success then
			    print(r.content)
			  end

			  -- Read a file
			  local r = await dass.tool.call("fs-read_entry", {
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

		public override void Populate(LuaTable globals, LuaTable ns, LuaService luaService)
		{
			ns["call"] = new LuaCallbackFunction(Call);
			ns["list"] = new LuaCallbackFunction(ListTools);
		}

		public async Task<LuaTuple> Call(LuaCallingContext ctx, LuaValue[] args)
		{
			if (args.Length < 1 || args[0] is not LuaString functionName)
				throw new LuaRuntimeException("dass.tool.call(name, args): first argument must be a string (tool name).");

			var arguments = args.Length > 1 ? args[1] : LuaNil.Instance;
			if (arguments is not LuaTable)
				arguments = new LuaTable();

			if (!_toolsetCache.AvailableTools.TryGetValue(functionName.Value, out var tool))
				throw new LuaRuntimeException($"Tool '{functionName.Value}' not found.");

			var jsonArgs = StructuredLuaConverter.LuaValueToJsonNode(arguments) ?? new JsonObject();
			var context = ctx.TryGetToolExecutionContext();
			if (context == null)
			{
				var chat = _services.GetRequiredService<Chat>();
				context = ToolExecutionContext.CreateDummy(tool, jsonArgs, chat);
			}
			var reactiveResult = await tool.Executor.Invoke(jsonArgs, context, CancellationToken.None);

			// Wait for completion to get success status
			var success = await reactiveResult.Completion;
			var content = reactiveResult.ResultContent;
			var structured = StructuredLuaConverter.JsonNodeToLuaValue(reactiveResult.StructuredResult);

			// Build structured result table
			var resultTable = new LuaTable();
			resultTable["content"] = new LuaString(content);
			resultTable["structured"] = structured;
			resultTable["success"] = LuaBoolean.FromBoolean(success);
			resultTable["tool"] = ToolToTable(tool);
			if (reactiveResult.StatusTitle != null)
				resultTable["status_title"] = new LuaString(reactiveResult.StatusTitle);
			if (reactiveResult.StatusIcon != null)
				resultTable["status_icon"] = new LuaString(reactiveResult.StatusIcon.ToString() ?? string.Empty);

			return new LuaTuple(resultTable);
		}

		public LuaTuple ListTools(LuaCallingContext ctx, LuaValue[] args)
		{
			var resultTable = new LuaTable();

			int i = 1;
			foreach (var (_, tool) in _toolsetCache.AvailableTools)
			{
				var entry = ToolToTable(tool);
				resultTable.Set(i++, entry);
			}

			return new LuaTuple(resultTable);
		}

		private static LuaTable ToolToTable(ToolInfo tool)
		{
			var result = new LuaTable();
			result["name"] = new LuaString(tool.Name);
			result["description"] = new LuaString(tool.DescriptionGetter());
			result["category"] = new LuaString(tool.Category);
			if (tool.DisplayName != null)
				result["display_name"] = new LuaString(tool.DisplayName);
			result["enabled"] = LuaBoolean.FromBoolean(tool.Enabled);
			result["approval_level"] = new LuaString(tool.ApprovalLevel.ToString());
			result["source"] = new LuaString(tool.Source.ToString().ToLower());
			result["arguments"] = StructuredLuaConverter.JsonNodeToLuaValue(tool.ArgumentSchema);
			return result;
		}
	}
}
