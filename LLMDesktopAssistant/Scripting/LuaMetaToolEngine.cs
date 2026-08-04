using System.Text.Json;
using System.Text.Json.Nodes;
using AsyncLua;
using AsyncLua.Values;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Tools.Meta;
using RCParsing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LLMDesktopAssistant.Scripting
{
	/// <summary>
	/// Lua implementation of <see cref="IMetaToolEngine"/>.
	/// Handles meta tools written in Lua with YAML frontmatter in `--[[ ... ]]` blocks.
	/// </summary>
	[ChatService(typeof(IMetaToolEngine))]
	public class LuaMetaToolEngine : IMetaToolEngine
	{
		private readonly LuaService _luaService;

		public ScriptLanguageType Language => ScriptLanguageType.Lua;

		public IMetaToolEngineDescriptor Descriptor { get; } = new LuaMetaToolEngineDescriptor();

		public LuaMetaToolEngine(LuaService luaService)
		{
			_luaService = luaService;
		}

		public Func<JsonNode?, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> CreateExecutor(MetaTool tool)
		{
			return (JsonNode? args, ToolExecutionContext context, CancellationToken cancellationToken) =>
			{
				var reactiveResult = new ReactiveToolResult();

				_ = Task.Run(async () =>
				{
					try
					{
						var scriptResult = await _luaService.ExecuteAsync(tool.ExecutionCode, print => reactiveResult.ResultContentLines.Add(print), g =>
						{
							g["tool_args"] = StructuredLuaConverter.JsonNodeToLuaValue(args);
							g[LuaVariables.ToolExecutionContext] = LuaValueConverter.ToLuaValue(context);
							g[LuaVariables.ToolReactiveResult] = LuaValueConverter.ToLuaValue(reactiveResult);
						});
						if (reactiveResult.StructuredResult == null)
							reactiveResult.StructuredResult = StructuredLuaConverter.LuaValueToJsonNode(scriptResult);
						reactiveResult.ResultContentLines.Add($"Script returned: " + scriptResult.ToString());
						reactiveResult.TryCompleteWithSuccess();
					}
					catch (LuaRuntimeException srex)
					{
						reactiveResult.ResultContentLines.Add("Caught error: " + srex.Message);
						reactiveResult.TryCompleteWithError();
					}
					catch (Exception ex)
					{
						reactiveResult.ResultContentLines.Add("Caught error: " + ex.Message);
						reactiveResult.TryCompleteWithError();
					}
					finally
					{
					}
				}, CancellationToken.None);

				return Task.FromResult(reactiveResult);
			};
		}
	}
}
