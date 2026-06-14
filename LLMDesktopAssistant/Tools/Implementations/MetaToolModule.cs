using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Scripting;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Tools.Implementations
{
	/// <summary>
	/// Module for managing meta tools — tools created by LLM at runtime.
	/// Supports multiple scripting languages (Lua, Python, etc.) via <see cref="IMetaToolEngine"/>.
	/// </summary>
	[ToolModule]
	public class MetaToolModule : ToolModule
	{
		private readonly IMetaToolManagementService _metaToolManager;
		private readonly IMetaToolEngine[] _engines;

		public MetaToolModule(IMetaToolManagementService metaToolManager, IEnumerable<IMetaToolEngine> engines)
		{
			_metaToolManager = metaToolManager;
			_engines = engines.ToArray();

			AddTool(CreateOrUpdateMetaTool,
				new ToolInitializationInfo
				{
					Name = "metatools-create_or_update",
					Description = BuildCreateOrUpdateDescription(),
					Category = "metatools",
					DefaultExpectedBehaviour = ToolBehaviour.ScriptAccess
				});

			AddTool(ListMetaTools,
				new ToolInitializationInfo
				{
					Name = "metatools-list",
					Description = "Lists all existing meta tools. Use it for understanding what tools you can tweak or modify.",
					Category = "metatools"
				});

			AddTool(GetToolInfo,
				new ToolInitializationInfo
				{
					Name = "metatools-get_info",
					Description = "Gets detailed information about a specific meta tool by its name. Use it for understanding the details of a particular tool.",
					Category = "metatools"
				});

			AddTool(RenameMetaTool,
				new ToolInitializationInfo
				{
					Name = "metatools-rename",
					Description = "Renames an existing meta tool to a new name. The original tool must exist and the new name must not conflict with any other tools.",
					Category = "metatools",
					DefaultExpectedBehaviour = ToolBehaviour.ScriptAccess
				});

			AddTool(DeleteMetaTool,
				new ToolInitializationInfo
				{
					Name = "metatools-delete",
					Description = "Removes an existing meta tool. The tool must exist and cannot be a default tool.",
					Category = "metatools",
					DefaultExpectedBehaviour = ToolBehaviour.ScriptAccess
				});
		}

		private string BuildCreateOrUpdateDescription()
		{
			var sb = new StringBuilder();
			sb.AppendLine("Creates or updates a meta tool with the specified details.");
			sb.AppendLine("If the tool already exists, it will be updated.");
			sb.AppendLine("If tool does not exist, all required parameters must be provided.");
			sb.AppendLine();
			sb.AppendLine("Available scripting languages:");

			foreach (var engine in _engines)
			{
				sb.AppendLine();
				sb.AppendLine($"--- {engine.Language} ---");
				sb.AppendLine($"File extension: {engine.FileExtension}");
				sb.AppendLine();
				sb.AppendLine("Example arguments:");
				sb.AppendLine(engine.ExampleArgs);
				sb.AppendLine();
				sb.AppendLine("Example code:");
				sb.AppendLine(engine.ExampleCode);
			}

			sb.AppendLine();
			sb.AppendLine("Notes:");
			sb.AppendLine("- Arguments can be accessed via the `tool_args` variable.");
			sb.AppendLine("- Use 'print' to return the result to the LLM.");
			sb.AppendLine("- The tool returns a pair of STDOUT and STDERR.");

			return sb.ToString();
		}

		public ToolResult CreateOrUpdateMetaTool(
			[Description("The name of the tool. Example: 'get_weather'")]
			string name,
			[Description("The scripting language to use. Example: 'Lua'")]
			string language,
			[Description("A description of what the tool does. This can contain guides for LLM how to use that tool.")]
			string? description = null,
			[Description("The human-readable title of the tool for showing it in the UI.")]
			string? title = null,
			[Description("The human-readable category of the tool for showing it in the UI.")]
			string? category = null,
			[Description("Whether the tool requires user confirmation before execution. Use 'true' for potentially dangerous actions. Use 'false' otherwise.")]
			bool? askForConfirmation = null,
			[Description(
				"""
				The JSON schema for the arguments that the tool accepts.
				Example:
				{
					"type": "object",
					"properties": {
						"location": {
							"type": "string",
							"description": "The location to check the weather for. Example: 'New York' or 'London'."
						}
					},
					"required": ["location"]
				}
				""")]
			string? argumentSchema = null,
			[Description("The code that will be executed when the tool is called. Check the description above for available languages and examples.")]
			string? executionCode = null)
		{
			try
			{
				if (!ToolName.CheckValid(name))
					return new ToolResult(ToolResultStatus.Error, "Invalid tool name. Please use alphanumeric characters, underscores and dashes.");

				// Parse language
				if (!Enum.TryParse<ScriptLanguageType>(language, ignoreCase: true, out var lang))
				{
					var validLanguages = string.Join(", ", _engines.Select(e => e.Language));
					return new ToolResult(ToolResultStatus.Error,
						$"Invalid language '{language}'. Valid languages: {validLanguages}.");
				}

				// Validate that we have an engine for this language
				if (!_engines.Any(e => e.Language == lang))
					return new ToolResult(ToolResultStatus.Error,
						$"Language '{language}' is not available. Valid languages: {string.Join(", ", _engines.Select(e => e.Language))}.");

				JsonObject? argumentSchemaJson;
				if (argumentSchema != null)
				{
					argumentSchemaJson = JsonNode.Parse(argumentSchema) as JsonObject;
					if (argumentSchemaJson == null)
						return new ToolResult(ToolResultStatus.Error, "Invalid argument schema. Must be a valid JSON object.");
				}
				else
				{
					argumentSchemaJson = null;
				}

				_metaToolManager.CreateOrUpdateTool(name, description, title, category,
					askForConfirmation, argumentSchemaJson, lang, executionCode);

				return new ToolResult($"Tool '{name}' ({lang}) created or updated successfully");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Failed to create or update tool: {ex.Message}");
			}
		}

		public ToolResult GetToolInfo(string name)
		{
			try
			{
				var tools = _metaToolManager.ListTools();
				var tool = tools.FirstOrDefault(t => t.Name == name);

				if (tool == null)
					return new ToolResult(ToolResultStatus.Error, $"No meta tool with name '{name}' found.");

				var result = new StringBuilder();

				result.AppendLine($"Tool: {tool.Name}");
				result.AppendLine($"Title: {tool.Title}");
				result.AppendLine($"Description: {tool.Description}");
				result.AppendLine($"Category: {tool.Category}");
				result.AppendLine($"Language: {tool.ScriptLanguage}");
				result.AppendLine($"Requires confirmation: {tool.AskForConfirmation}");

				result.AppendLine().AppendLine("Argument schema:");
				result.AppendLine(tool.ArgumentSchema?.ToJsonString(new JsonSerializerOptions
				{
					WriteIndented = true
				}) ?? "{}");

				result.AppendLine().AppendLine($"{tool.ScriptLanguage} execution code:");
				result.AppendLine(tool.ExecutionCode);

				return new ToolResult(result.ToString());
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Failed to get tool info: {ex.Message}");
			}
		}

		public ToolResult ListMetaTools()
		{
			try
			{
				var tools = _metaToolManager.ListTools();

				if (tools.Length == 0)
					return new ToolResult("No meta tools found.");

				var result = new StringBuilder();
				result.AppendLine("Existing meta tools:");

				foreach (var tool in tools)
				{
					result.AppendLine($"- {tool.Name} ({tool.ScriptLanguage}) — {tool.Title}");
				}

				return new ToolResult(result.ToString());
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Failed to list tools: {ex.Message}");
			}
		}

		public ToolResult RenameMetaTool(
			[Description("The current name of the meta tool")]
			string oldName,
			[Description("The new name for the meta tool")]
			string newName)
		{
			try
			{
				if (!ToolName.CheckValid(newName))
					return new ToolResult(ToolResultStatus.Error, "Invalid new tool name. Please use alphanumeric characters, underscores and dashes.");

				_metaToolManager.RenameTool(oldName, newName);
				return new ToolResult($"Tool '{oldName}' renamed to '{newName}' successfully.");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Failed to rename tool: {ex.Message}");
			}
		}

		public ToolResult DeleteMetaTool(
			[Description("The name of the meta tool to delete")]
			string name)
		{
			try
			{
				_metaToolManager.DeleteTool(name);
				return new ToolResult($"Tool '{name}' deleted successfully.");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Failed to delete tool: {ex.Message}");
			}
		}
	}
}
