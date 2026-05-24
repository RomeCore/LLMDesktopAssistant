using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels.Tools;
using RCParsing;
using YamlDotNet.Serialization.NamingConventions;

namespace LLMDesktopAssistant.Desktop.Services
{
	[ChatService(typeof(IMetaToolManagementService))]
	public class MetaToolManagementService(Chat chat) : IMetaToolManagementService
	{
		private static readonly Parser _frontmatterParser;
		private static readonly YamlDotNet.Serialization.ISerializer _frontmatterSerializer;
		private static readonly YamlDotNet.Serialization.IDeserializer _frontmatterDeserializer;
		private static readonly JsonSerializerOptions _argumentSchemaSerializerOptions;

		private class FrontmatterDto
		{
			public string Title { get; set; } = string.Empty;
			public string Description { get; set; } = string.Empty;
			public string Category { get; set; } = string.Empty;
			public bool AskForConfirmation { get; set; } = false;
			public string ArgumentSchema { get; set; } = string.Empty;
		}

		static MetaToolManagementService()
		{
			var pb = new ParserBuilder();

			pb.Settings.Skip(b => b.Whitespaces(), ParserSkippingStrategy.TryParseThenSkip);

			pb.CreateRule("python_frontmatter")
				.Literal("\"\"\"")
				.TextUntil("\"\"\"")
				.Literal("\"\"\"")
				.AllText();

			_frontmatterParser = pb.Build();

			_frontmatterSerializer = new YamlDotNet.Serialization.SerializerBuilder()
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			_frontmatterDeserializer = new YamlDotNet.Serialization.DeserializerBuilder()
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			_argumentSchemaSerializerOptions = new JsonSerializerOptions
			{
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
				WriteIndented = true
			};
		}

		private readonly PythonService _python = ServiceRegistry.Get<PythonService>();

		public void CreateOrUpdateTool(string name,
			string? description, string? title, string? category, bool? askForConfirmation,
			JsonObject? argumentSchema, ScriptLanguageType? language, string? executionCode)
		{
			var toolToUpdate = TryGetTool(name);
			if (toolToUpdate == null)
			{
				List<string> nullArguments = [];

				if (description is null) nullArguments.Add(nameof(description));
				if (title is null) title = name;
				if (category is null) category = "General Meta";
				if (askForConfirmation is null) askForConfirmation = false;
				if (language is null) nullArguments.Add(nameof(language));
				if (executionCode is null) nullArguments.Add(nameof(executionCode));

				if (nullArguments.Count > 0)
				{
					throw new ArgumentException(
						$"When creating a new tool, the following arguments cannot be null: {string.Join(", ", nullArguments)}");
				}

				toolToUpdate = new MetaTool
				{
					Name = name,
					Title = title,
					Description = description!,
					Category = category,
					AskForConfirmation = askForConfirmation.Value,
					ArgumentSchema = argumentSchema!,
					ScriptLanguage = language!.Value,
					ExecutionCode = executionCode!,
				};
				WriteTool(toolToUpdate);
				return;
			}

			toolToUpdate = new MetaTool
			{
				Name = name,
				Title = title ?? toolToUpdate.Title,
				Description = description ?? toolToUpdate.Description,
				Category = category ?? toolToUpdate.Category,
				AskForConfirmation = askForConfirmation ?? toolToUpdate.AskForConfirmation,
				ArgumentSchema = argumentSchema ?? toolToUpdate.ArgumentSchema,
				ScriptLanguage = language ?? toolToUpdate.ScriptLanguage,
				ExecutionCode = executionCode ?? toolToUpdate.ExecutionCode
			};
			WriteTool(toolToUpdate);
		}

		private MetaTool DeserializeTool(string file)
		{
			var name = Path.GetFileNameWithoutExtension(file);
			var extension = Path.GetExtension(file);

			if (extension == ".py")
			{
				var parsed = _frontmatterParser.ParseRule("python_frontmatter", File.ReadAllText(file));
				var frontmatterText = parsed[1].Text.Trim();
				var executionCode = parsed[3].Text.Trim();

				var frontmatter = _frontmatterDeserializer.Deserialize<FrontmatterDto>(frontmatterText);
				var argumentSchema = JsonSerializer.Deserialize<JsonObject>(frontmatter.ArgumentSchema, _argumentSchemaSerializerOptions)!;

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
			else
			{
				throw new ArgumentException($"Unsupported tool file extension: {extension}");
			}
		}

		public void WriteTool(MetaTool tool)
		{
			if (tool.ScriptLanguage == ScriptLanguageType.Python)
			{
				var argumentSchemaText = JsonSerializer.Serialize(tool.ArgumentSchema, _argumentSchemaSerializerOptions);
				var frontmatter = new FrontmatterDto
				{
					Title = tool.Title,
					Description = tool.Description,
					Category = tool.Category,
					AskForConfirmation = tool.AskForConfirmation,
					ArgumentSchema = argumentSchemaText
				};
				var frontmatterText = _frontmatterSerializer.Serialize(frontmatter);
				var contents = $""""
					"""
					{frontmatterText}
					"""
					{tool.ExecutionCode}
					"""";
				File.WriteAllText(Path.Combine(Directories.Metatools, $"{tool.Name}.py"), contents);
			}
			else
			{
				throw new ArgumentException($"Unsupported script language: {tool.ScriptLanguage}");
			}
		}

		public MetaTool? TryGetTool(string name)
		{
			var file = Directory.GetFiles(Directories.Metatools).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == name);
			if (file == null)
				return null;
			return DeserializeTool(file);
		}

		public MetaTool GetTool(string name)
		{
			var file = Directory.GetFiles(Directories.Metatools).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == name);
			if (file == null)
				throw new KeyNotFoundException($"Could not find a tool with the name '{name}'");
			return DeserializeTool(file);
		}

		public MetaTool[] ListTools()
		{
			var files = Directory.GetFiles(Directories.Metatools);
			var tools = files.Select(DeserializeTool);
			return tools.OrderBy(t => t.Category).ThenBy(t => t.Title).ToArray();
		}

		public void RenameTool(string oldName, string newName)
		{
			var oldFile = Directory.GetFiles(Directories.Metatools).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == oldName);
			if (oldFile == null)
				throw new KeyNotFoundException($"Could not find a tool with the name '{oldFile}'");
			var extension = Path.GetExtension(oldFile);
			var newFile = Path.Combine(Directories.Metatools, newName + extension);
			if (File.Exists(newFile))
				throw new InvalidOperationException($"A tool with the name '{newName}' already exists.");
			File.Move(oldFile, newFile);
		}

		public void DeleteTool(string name)
		{
			var file = Directory.GetFiles(Directories.Metatools).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == name);
			if (file == null)
				throw new KeyNotFoundException($"Could not find a tool with the name '{name}'");
			File.Delete(file);
		}

		public ToolInfo[] GetMetaTools()
		{
			var result = new List<ToolInfo>();

			foreach (var tool in ListTools())
			{
				var desc = tool.Description;
				result.Add(new ToolInfo
				{
					Name = tool.Name,
					DescriptionGetter = () => desc,
					ArgumentSchema = tool.ArgumentSchema ?? new JsonObject(),
					Executor = CreateExecutor(tool),
					DisplayName = tool.Title,
					Category = tool.Category,
					Source = ToolSource.Meta,
					AskForConfirmation = tool.AskForConfirmation,
					Enabled = true
				});
			}

			return result.ToArray();
		}

		private Func<JsonNode, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> CreateExecutor(MetaTool metaTool)
		{
			if (metaTool.ScriptLanguage == ScriptLanguageType.Python)
			{
				async Task<ReactiveToolResult> ExecuteAsync(JsonNode args, ToolExecutionContext context, CancellationToken cancellationToken)
				{
					try
					{
						string pythonCode = $"""
						tool_args = {SerializeNodeToPython(args)}
						{metaTool.ExecutionCode}
						""";

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
						return ReactiveToolResult.Create(success, resultBuilder.ToString());
					}
					catch (Exception ex)
					{
						return ReactiveToolResult.CreateError($"An error occurred while executing the tool: {ex.Message}");
					}
				}

				return ExecuteAsync;
			}
			else
			{
				throw new NotSupportedException($"Script language '{metaTool.ScriptLanguage}' is not supported.");
			}
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