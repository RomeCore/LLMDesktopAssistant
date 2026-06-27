using System.Text.Json;
using System.Text.Json.Nodes;
using AsyncLua;
using AsyncLua.Values;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Tools;
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
		private static readonly Parser _frontmatterParser;

		private static readonly ISerializer _yamlSerializer = new SerializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

		private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

		private static readonly JsonSerializerOptions _jsonOptions = new()
		{
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
			WriteIndented = true
		};

		static LuaMetaToolEngine()
		{
			var pb = new ParserBuilder();
			pb.Settings.Skip(b => b.Whitespaces(), ParserSkippingStrategy.TryParseThenSkip);
			pb.CreateRule("lua_frontmatter")
				.Literal("--[[")
				.TextUntil("]]")
				.Literal("]]")
				.AllText();
			_frontmatterParser = pb.Build();
		}

		private readonly LuaService _luaService;

		public ScriptLanguageType Language => ScriptLanguageType.Lua;
		public string FileExtension => ".lua";

		public string ExampleArgs => """
			{"location": "New York", "days": 3}
			""";

		public string ExampleCode => """
			-- Fetch weather data
			local url = "https://api.weather.com/forecast?q=" .. tool_args.location .. "&days=" .. tool_args.days
			local result = web.fetch(url)
			print("Weather in " .. tool_args.location .. ": " .. result)
			""";

		public LuaMetaToolEngine(LuaService luaService)
		{
			_luaService = luaService;
		}

		private class FrontmatterDto
		{
			public string Title { get; set; } = string.Empty;
			public string Description { get; set; } = string.Empty;
			public string Category { get; set; } = string.Empty;
			public bool AskForConfirmation { get; set; } = false;
			public string ArgumentSchema { get; set; } = string.Empty;
		}

		public MetaTool Deserialize(string fileContent, string name)
		{
			var parsed = _frontmatterParser.ParseRule("lua_frontmatter", fileContent);
			var frontmatterText = parsed[1].Text.Trim();
			var executionCode = parsed[3].Text.Trim();

			var frontmatter = _yamlDeserializer.Deserialize<FrontmatterDto>(frontmatterText);
			var argumentSchema = JsonSerializer.Deserialize<JsonObject>(frontmatter.ArgumentSchema, _jsonOptions)
				?? new JsonObject { ["type"] = "object", ["additionalProperties"] = false };

			return new MetaTool
			{
				Name = name,
				Title = frontmatter.Title,
				Description = frontmatter.Description,
				Category = frontmatter.Category,
				AskForConfirmation = frontmatter.AskForConfirmation,
				ArgumentSchema = argumentSchema,
				ScriptLanguage = ScriptLanguageType.Lua,
				ExecutionCode = executionCode
			};
		}

		public string Serialize(MetaTool tool)
		{
			var argumentSchemaText = JsonSerializer.Serialize(tool.ArgumentSchema, _jsonOptions);
			var frontmatter = new FrontmatterDto
			{
				Title = tool.Title,
				Description = tool.Description,
				Category = tool.Category,
				AskForConfirmation = tool.AskForConfirmation,
				ArgumentSchema = argumentSchemaText
			};
			var frontmatterText = _yamlSerializer.Serialize(frontmatter);

			return $"""
				--[[
				{frontmatterText.TrimEnd()}
				]]
				{tool.ExecutionCode}
				""";
		}

		public Func<JsonNode, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> CreateExecutor(MetaTool tool)
		{
			return (JsonNode args, ToolExecutionContext context, CancellationToken cancellationToken) =>
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
