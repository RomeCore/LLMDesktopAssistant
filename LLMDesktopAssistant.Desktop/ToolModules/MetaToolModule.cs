using DocumentFormat.OpenXml.Wordprocessing;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LLMDesktopAssistant.Desktop.ToolModules
{
	[ToolModule]
	public class MetaToolModule : ToolModule
	{
		private readonly IMetaToolManagementService _metaToolManager;

		public MetaToolModule(IMetaToolManagementService metaToolManager)
		{
			_metaToolManager = metaToolManager;

			AddTool(CreateOrUpdateMetaTool,
				new ToolInitializationInfo
				{
					Name = "metatools-create_or_update",
					Description = """
						Creates or updates a meta tool with the specified details.
						If the tool already exists, it will be updated.
						If tool does not exists, all parameters must not be null.
						""",
					Category = "metatools",
					AskForConfirmation = true
				});

			AddTool(ListMetaTools,
				new ToolInitializationInfo
				{
					Name = "metatools-list",
					Description = "Lists all existing meta tools. Use it for understanding what tools you can tweak or modify.",
					Category = "metatools",
					AskForConfirmation = false
				});

			AddTool(GetToolInfo,
				new ToolInitializationInfo
				{
					Name = "metatools-get_info",
					Description = "Gets detailed information about a specific meta tool by its name. Use it for understanding the details of a particular tool.",
					Category = "metatools",
					AskForConfirmation = false
				});

			AddTool(RenameMetaTool,
				new ToolInitializationInfo
				{
					Name = "metatools-rename",
					Description = "Renames an existing meta tool to a new name. The original tool must exist and the new name must not conflict with any other tools.",
					Category = "metatools",
					AskForConfirmation = true
				});

			AddTool(DeleteMetaTool,
				new ToolInitializationInfo
				{
					Name = "metatools-delete",
					Description = "Removes an existing meta tool. The tool must exist and cannot be a default tool.",
					Category = "metatools",
					AskForConfirmation = true
				});
		}

		public ToolResult CreateOrUpdateMetaTool(
			[Description("The name of the tool. Example: 'get_weather'")]
			string name,
			[Description("A description of what the tool does. This can conain guides for LLM how to use that tool.")]
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
			[Description(
				"""
				The Python code that will be executed when the tool is called.
				Arguments can be accessed via the `tool_args` variable.
				In result, tool will return a pair of STDOUT and STDERR of Python execution,
				meaning that you can use 'print' to return the result.

				Example:
				import python_weather
				import asyncio

				async def getweather():
				    async with python_weather.Client() as client:
				        location = tool_args["location"]
				        weather = await client.get(location)
				        print(f"Current temperature: {weather.temperature}°C")

				asyncio.run(getweather())
				""")]
			string? pythonExecutionCode = null)
		{
			try
			{
				if (!ToolName.CheckValid(name))
					return new ToolResult(ToolResultStatus.Error, "Invalid tool name. Please use alphanumeric characters, underscores and dashes.");

				JsonObject? argumentSchemaJson;
				if (argumentSchema != null)
				{
					argumentSchemaJson = JsonNode.Parse(argumentSchema) as JsonObject;
					if (argumentSchemaJson == null)
						return new ToolResult(ToolResultStatus.Error, "Invalid argument schema");
				}
				else
				{
					argumentSchemaJson = null;
				}

				_metaToolManager.CreateOrUpdateTool(name, description, title, category, askForConfirmation, argumentSchemaJson, Scripting.ScriptLanguageType.Python, pythonExecutionCode);
				return new ToolResult($"Tool '{name}' created or updated successfully");
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
				return new ToolResult(ToolResultStatus.Error, $"Failed to list tools: {ex.Message}");
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
					result.AppendLine($"- {tool.Name}");
					result.AppendLine($"  Title: {tool.Title}");
					result.AppendLine($"  Description: {tool.Description}");
					result.AppendLine($"  Category: {tool.Category}");
					result.AppendLine($"  Requires confirmation: {tool.AskForConfirmation}");
					result.AppendLine();
				}

				return new ToolResult(result.ToString().Trim());
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Failed to list tools: {ex.Message}");
			}
		}

		public ToolResult RenameMetaTool(string oldName, string newName)
		{
			try
			{
				_metaToolManager.RenameTool(oldName, newName);
				return new ToolResult($"Tool '{oldName}' renamed to '{newName}' successfully");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Failed to rename tool: {ex.Message}");
			}
		}

		public ToolResult DeleteMetaTool(string name)
		{
			try
			{
				_metaToolManager.DeleteTool(name);
				return new ToolResult($"Deleted tool '{name}' successfully");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Failed to delete tool: {ex.Message}");
			}
		}
	}
}