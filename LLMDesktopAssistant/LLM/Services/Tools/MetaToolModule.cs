using DocumentFormat.OpenXml.Wordprocessing;
using LLMDesktopAssistant.ToolModules;
using RCLargeLanguageModels.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	public class MetaToolModule : ToolModule
	{
		private readonly IMetaToolManagementService _metaToolManager;

		public MetaToolModule(IMetaToolManagementService metaToolManager)
		{
			_metaToolManager = metaToolManager;

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(CreateOrUpdateMetaTool,
					"tools-create_or_update",
					"""
					Creates or updates a meta tool with the specified details.
					If the tool already exists, it will be updated.
					If tool does not exists, all parameters must not be null.
					"""),
				Category = "metatools",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ListMetaTools,
					"tools-list",
					"Lists all existing meta tools names. Use it for understanding what tools you can tweak or modify."),
				Category = "metatools",
				AskForConfirmation = false
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(RenameMetaTool,
					"tools-rename",
					"Renames an existing meta tool to a new name. The original tool must exist and the new name must not conflict with any other tools."),
				Category = "metatools",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(DeleteMetaTool,
					"tools-delete",
					"Removes an existing meta tool. The tool must exist and cannot be a default tool."),
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

				_metaToolManager.CreateOrUpdateTool(name, description, title, category, askForConfirmation, argumentSchemaJson, pythonExecutionCode);
				return new ToolResult($"Tool '{name}' created or updated successfully");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Failed to create or update tool: {ex.Message}");
			}
		}

		public ToolResult ListMetaTools()
		{
			try
			{
				var names = _metaToolManager.ListToolNames();
				return new ToolResult($"Tools: {string.Join(", ", names)}");
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