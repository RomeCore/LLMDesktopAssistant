using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.ToolModules;
using RCLargeLanguageModels.Tools;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Tools;

namespace LLMDesktopAssistant.Desktop.Services
{
	[ChatService(typeof(IMetaToolManagementService))]
	public class MetaToolManagementService(Chat chat) : IMetaToolManagementService
	{
		private readonly MetaToolConfiguration _configuration = SettingsManager.Get<MetaToolConfiguration>();
		private readonly PythonService _python = ServiceRegistry.Get<PythonService>();

		public void CreateOrUpdateTool(string name,
			string? description, string? title, string? category, bool? askForConfirmation,
			JsonObject? argumentSchema, string? pythonExecutionCode)
		{
			var toolToUpdate = _configuration.Tools.FirstOrDefault(t => t.Name == name);
			if (toolToUpdate == null)
			{
				List<string> nullArguments = [];

				if (description is null) nullArguments.Add(nameof(description));
				if (title is null) title = name;
				if (category is null) category = "General Meta";
				if (askForConfirmation is null) askForConfirmation = false;
				if (argumentSchema is null) nullArguments.Add(nameof(argumentSchema));
				if (pythonExecutionCode is null) nullArguments.Add(nameof(pythonExecutionCode));

				if (nullArguments.Count > 0)
				{
					throw new ArgumentException(
						$"When creating a new tool, the following arguments cannot be null: {string.Join(", ", nullArguments)}");
				}

				toolToUpdate = new MetaTool
				{
					Name = name
				};
				_configuration.Tools.Add(toolToUpdate);
			}

			toolToUpdate.Description = description ?? toolToUpdate.Description;
			toolToUpdate.Title = title ?? toolToUpdate.Title;
			toolToUpdate.Category = category ?? toolToUpdate.Category;
			toolToUpdate.AskForConfirmation = askForConfirmation ?? toolToUpdate.AskForConfirmation;
			toolToUpdate.ArgumentSchema = argumentSchema ?? toolToUpdate.ArgumentSchema;
			toolToUpdate.PythonExecutionCode = pythonExecutionCode ?? toolToUpdate.PythonExecutionCode;
		}

		public MetaTool[] ListTools()
		{
			return _configuration.Tools.OrderBy(t => t.Category).ThenBy(t => t.Title).ToArray();
		}

		public void RenameTool(string oldName, string newName)
		{
			if (_configuration.Tools.Any(t => t.Name == newName))
				throw new InvalidOperationException($"Tool with the same name '{newName}' already exists.");

			var toolToRename = _configuration.Tools.FirstOrDefault(t => t.Name == oldName);
			if (toolToRename == null)
				throw new InvalidOperationException($"Tool with the name '{oldName}' does not exist.");

			toolToRename.Name = newName;
		}

		public void DeleteTool(string name)
		{
			var toolToDelete = _configuration.Tools.FirstOrDefault(t => t.Name == name);
			if (toolToDelete == null)
				throw new InvalidOperationException($"Tool with the name '{name}' does not exist.");

			_configuration.Tools.Remove(toolToDelete);
		}

		public ToolInfo[] GetMetaTools()
		{
			var result = new List<ToolInfo>();

			foreach (var tool in _configuration.Tools.OrderBy(t => t.Category).ThenBy(t => t.Title))
			{
				result.Add(new ToolInfo
				{
					Tool = CreateFunctionTool(tool),
					DisplayName = tool.Title,
					Category = tool.Category,
					Source = ToolSource.Meta,
					AskForConfirmation = tool.AskForConfirmation,
					Enabled = true
				});
			}

			return result.ToArray();
		}

		private FunctionTool CreateFunctionTool(MetaTool metaTool)
		{
			async Task<ToolResult> ExecuteAsync(JsonNode arguments, CancellationToken cancellationToken)
			{
				try
				{
					string pythonCode = $"""
						# Python dictionaries, arrays and values are looking exact as JSON.
						# So we can simply serialize the JSON and put into the tool_args variable.
						tool_args = {SerializeNodeToPython(arguments)}

						{metaTool.PythonExecutionCode}
						""";

					var workDir = chat.Settings.GetWorkingDirectory();
					var activationScript = chat.Settings.PythonVenvActivateScriptPath;
					var result = await _python.RunScript(pythonCode, workDir, activationScript, cancellationToken);

					var resultBuilder = new StringBuilder();
					resultBuilder.Append(result.StdOut);
					if (!string.IsNullOrEmpty(result.StdErr))
					{
						resultBuilder.AppendLine().AppendLine("Errors:");
						resultBuilder.Append(result.StdErr);
					}

					bool success = result.Success && !result.StdOut.StartsWith("error", StringComparison.OrdinalIgnoreCase);
					var status = success ? ToolResultStatus.Success : ToolResultStatus.Error;
					return new ToolResult(status, resultBuilder.ToString());
				}
				catch (AggregateException aex) when (aex.InnerExceptions.Any(e => e is OperationCanceledException))
				{
					return new ToolResult(ToolResultStatus.Cancelled, "Tool execution was cancelled.");
				}
				catch (OperationCanceledException)
				{
					return new ToolResult(ToolResultStatus.Cancelled, "Tool execution was cancelled.");
				}
				catch (Exception ex)
				{
					return new ToolResult(ToolResultStatus.Error, $"An error occurred while executing the tool: {ex.Message}");
				}
			}

			return new FunctionTool(metaTool.Name, metaTool.Description, metaTool.ArgumentSchema, ExecuteAsync);
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