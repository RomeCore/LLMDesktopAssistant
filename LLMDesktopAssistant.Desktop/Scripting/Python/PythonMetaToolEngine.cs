using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Desktop.Execution;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using RCParsing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LLMDesktopAssistant.Desktop.Scripting.Python
{
	/// <summary>
	/// Python implementation of <see cref="IMetaToolEngine"/>.
	/// Handles meta tools written in Python with YAML frontmatter in `"""` docstring blocks.
	/// Requires Python runtime and optional virtual environment.
	/// Only available on Desktop platform.
	/// </summary>
	[Service(typeof(IMetaToolEngine))]
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

		private readonly IProcessLauncher _processLauncher;

		public ScriptLanguageType Language => ScriptLanguageType.Python;
		public string FileExtension => ".py";

		public PythonMetaToolEngine(IProcessLauncher processLauncher)
		{
			_processLauncher = processLauncher;
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

		public Func<JsonNode?, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> CreateExecutor(MetaTool tool)
		{
			return async (JsonNode? args, ToolExecutionContext context, CancellationToken cancellationToken) =>
			{
				try
				{
					string pythonCode = $"""
						import sys
						sys.stdout.reconfigure(encoding="utf-8")
						sys.stderr.reconfigure(encoding="utf-8")
						tool_args = {SerializeNodeToPython(args)}
						{tool.ExecutionCode}
						""";

					var chat = context.Chat;
					var workDir = chat.Settings.Environment.GetWorkingDirectory();
					var pythonConfig = chat.Settings.Environment.EnsureAdditional<PythonEnvironmentConfiguration>();
					var activationScript = pythonConfig.PythonMetaVenvActivateScriptPath
						?? pythonConfig.PythonVenvActivateScriptPath;

					var tempPyFile = Path.GetFullPath(Path.Combine(Directories.TempScripts, $"{Guid.NewGuid()}.py"));
					File.WriteAllText(tempPyFile, pythonCode);

					ProcessDescriptor? process = null;
					try
					{
						string command;
						if (!string.IsNullOrWhiteSpace(activationScript))
							command = $"call \"{activationScript}\" && python \"{tempPyFile}\"";
						else
							command = $"python \"{tempPyFile}\"";

						process = _processLauncher.Launch(new ProcessLaunchParameters
						{
							ProcessName = "Python Meta",
							FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
							Arguments = OperatingSystem.IsWindows() ? [$"/c \"{command}\""] : ["-c", command],
							VerbatimArguments = OperatingSystem.IsWindows(),
							WorkingDirectory = workDir
						}, cancellationToken);

						int exitCode = await process;
						return ReactiveToolResult.Create(exitCode == 0, process.Output + $"\nProcess exited with code {exitCode}. Check terminal output above for details.");
					}
					catch (Exception ex)
					{
						if (process != null)
							return ReactiveToolResult.CreateError(process.Output + $"\nProcess finished with error: {ex.Message}");
						else
							return ReactiveToolResult.CreateError(ex.Message);
					}
					finally
					{
						File.Delete(tempPyFile);
					}
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
