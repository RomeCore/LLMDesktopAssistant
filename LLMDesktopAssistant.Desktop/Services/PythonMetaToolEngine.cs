using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using RCParsing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LLMDesktopAssistant.Desktop.Services
{
	/// <summary>
	/// Python implementation of <see cref="IMetaToolEngine"/>.
	/// Handles meta tools written in Python with YAML frontmatter in `"""` docstring blocks.
	/// Requires Python runtime and optional virtual environment.
	/// Only available on Desktop platform.
	/// </summary>
	[Service(ServiceType = typeof(IMetaToolEngine))]
	public class PythonMetaToolEngine : IMetaToolEngine
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

		static PythonMetaToolEngine()
		{
			var pb = new ParserBuilder();
			pb.Settings.Skip(b => b.Whitespaces(), ParserSkippingStrategy.TryParseThenSkip);
			pb.CreateRule("python_frontmatter")
				.Literal("\"\"\"")
				.TextUntil("\"\"\"")
				.Literal("\"\"\"")
				.AllText();
			_frontmatterParser = pb.Build();
		}

		private readonly PythonService _python;

		public ScriptLanguageType Language => ScriptLanguageType.Python;
		public string FileExtension => ".py";

		public PythonMetaToolEngine(PythonService python)
		{
			_python = python;
		}

		private class FrontmatterDto
		{
			public string Title { get; set; } = string.Empty;
			public string Description { get; set; } = string.Empty;
			public string Category { get; set; } = string.Empty;
			public bool AskForConfirmation { get; set; } = false;
			public string ArgumentSchema { get; set; } = string.Empty;
		}

		public string ExampleArgs => """
			{"location": "New York", "days": 3}
			""";

		public string ExampleCode => """
			import python_weather
			import asyncio

			async def getweather():
			    async with python_weather.Client() as client:
			        location = tool_args["location"]
			        weather = await client.get(location)
			        print(f"Current temperature: {weather.temperature}°C")

			asyncio.run(getweather())
			""";


		public MetaTool Deserialize(string fileContent, string name)
		{
			var parsed = _frontmatterParser.ParseRule("python_frontmatter", fileContent);
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
				ScriptLanguage = ScriptLanguageType.Python,
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

			return $""""
				"""
				{frontmatterText.TrimEnd()}
				"""
				{tool.ExecutionCode}
				"""";
		}

		public Func<JsonNode, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> CreateExecutor(MetaTool tool)
		{
			return async (JsonNode args, ToolExecutionContext context, CancellationToken cancellationToken) =>
			{
				try
				{
					string pythonCode = $"""
						tool_args = {SerializeNodeToPython(args)}
						{tool.ExecutionCode}
						""";

					var chat = context.Chat;
					var workDir = chat.Settings.Environment.GetWorkingDirectory();
					var activationScript = chat.Settings.Environment.PythonVenvActivateScriptPath;
					var result = await _python.RunScript(pythonCode, workDir, activationScript, cancellationToken);

					var resultBuilder = new StringBuilder();
					resultBuilder.Append(result.StdOut);
					if (!string.IsNullOrEmpty(result.StdErr))
					{
						resultBuilder.AppendLine().AppendLine("Errors:");
						resultBuilder.Append(result.StdErr);
					}

					bool success = result.Success && !result.StdOut.StartsWith("error", StringComparison.OrdinalIgnoreCase);
					return ReactiveToolResult.Create(success, resultBuilder.ToString().TrimEnd());
				}
				catch (Exception ex)
				{
					return ReactiveToolResult.CreateError($"Python execution error: {ex.Message}");
				}
			};
		}

		private static string SerializeNodeToPython(JsonNode? node)
		{
			if (node == null)
				return "None";

			return node switch
			{
				JsonValue value => SerializeValueToPython(value),
				JsonObject obj => SerializeObjectToPython(obj),
				JsonArray arr => SerializeArrayToPython(arr),
				_ => throw new NotSupportedException($"Unsupported node type: {node.GetType()}")
			};
		}

		private static string SerializeValueToPython(JsonValue value)
		{
			switch (value.GetValueKind())
			{
				case JsonValueKind.Null:
					return "None";
				case JsonValueKind.True:
					return "True";
				case JsonValueKind.False:
					return "False";
				default:
					return value.ToJsonString();
			}
		}

		private static string SerializeObjectToPython(JsonObject obj)
		{
			var parts = new List<string>();

			foreach (var kvp in obj)
			{
				string key = SerializeValueToPython(JsonValue.Create(kvp.Key));
				string value = SerializeNodeToPython(kvp.Value);
				parts.Add($"{key}: {value}");
			}

			return "{" + string.Join(", ", parts) + "}";
		}

		private static string SerializeArrayToPython(JsonArray arr)
		{
			var items = new List<string>();

			foreach (var item in arr)
			{
				items.Add(SerializeNodeToPython(item));
			}

			return "[" + string.Join(", ", items) + "]";
		}
	}
}
