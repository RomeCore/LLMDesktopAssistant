using AsyncLua;
using AsyncLua.Values;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Services.Instances;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[LuaApi(chatScoped: false)]
	public class LuaApiModels : LuaApiBaseAsync
	{
		public override string? Namespace => "dass.models";

		public override string? Manuals => """
			--- dass.models — model listing API

			Provides access to the list of available LLM models.

			FUNCTIONS:

			--- dass.models.list()
			  Returns a table with detailed information about all available models.

			  Returns: table — array of model info tables. Each entry contains:
			    - client_name: string — provider/client identifier (e.g. "openai")
			    - client_display_name: string — provider display name (e.g. "OpenAI")
			    - name: string — model name (e.g. "gpt-4o")
			    - display_name: string — model display name (e.g. "GPT 4o")
			    - full_name: string — full model identifier (e.g. "openai$gpt-4o")

			EXAMPLES:

			  -- List all models
			  local models = dass.models.list()
			  for _, m in ipairs(models) do
			    print(m.full_name .. " (" .. m.client_display_name .. ")")
			  end

			NOTES:
			  - Models are sourced from all configured providers (OpenAI, Ollama, DeepSeek, OpenRouter, etc.).
			  - The list is refreshed automatically at startup.
			""";

		private readonly LLModelListService _modelList;

		public LuaApiModels(LLModelListService modelList)
		{
			_modelList = modelList;
		}

		public override void Populate(LuaTable globals, LuaTable ns, LuaService luaService)
		{
			ns["list"] = new LuaCallbackFunction(ListModels);
		}

		private LuaTuple ListModels(LuaCallingContext ctx, LuaValue[] args)
		{
			var models = _modelList.Registry.Models;
			var resultTable = new LuaTable();

			int i = 1;
			foreach (var model in models)
			{
				var entry = new LuaTable();
entry["client_name"] = new LuaString(model.Client?.Name ?? "");
			entry["client_display_name"] = new LuaString(model.Client?.DisplayName ?? "");
				entry["name"] = new LuaString(model.Name);
				entry["display_name"] = new LuaString(model.DisplayName);
				entry["full_name"] = new LuaString(model.FullName);
				resultTable.Set(i++, entry);
			}

			return new LuaTuple(resultTable);
		}
	}
}
