using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	[Service(typeof(IPromptDumpService))]
	public class PromptDumpService : IPromptDumpService
	{
		/// <summary>
		/// Gets or sets a value indicating whether prompt dumps are enabled (off by default).
		/// </summary>
		public static bool IsEnabled { get; set; }

		/// <inheritdoc/>
		public void Dump(IEnumerable<IMessage> messages, IEnumerable<ITool> tools, string? scmContext = null)
		{
			var dumpPath = Path.Combine(Directories.LocalAppData, "dumps");
			Directory.CreateDirectory(dumpPath);

			var dumpFile = Path.Combine(dumpPath, $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}.json");
			var serializerSettings = new JsonSerializerOptions
			{
				WriteIndented = true,
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
				TypeInfoResolver = new DefaultJsonTypeInfoResolver()
			};

			var payload = new JsonObject
			{
				["scm"] = scmContext,
				["messages"] = new JsonArray(messages.Select(FromMessage).ToArray()),
				["tools"] = new JsonArray(tools.Select(FromTool).ToArray())
			};
			var json = JsonSerializer.Serialize(payload, serializerSettings);
			File.WriteAllText(dumpFile, json);
		}

		private JsonObject FromMessage(IMessage message)
		{
			var result = new JsonObject
			{
				["role"] = message.Role.ToString().ToLower(),
				["content"] = message.Content
			};

			if (message is IToolMessage toolMessage)
			{
				result["tool_call_id"] = toolMessage.ToolCallId;
				result["tool_name"] = toolMessage.ToolName;
			}

			if (message is IAssistantMessage assistantMessage)
			{
				var toolCalls = new JsonArray();
				foreach (var toolCall in assistantMessage.ToolCalls)
				{
					var toolCallResult = new JsonObject
					{
						["tool_call_id"] = toolCall.Id,
						["tool_name"] = toolCall.ToolName
					};

					if (toolCall is IFunctionToolCall functionToolCall)
					{
						toolCallResult["tool_type"] = "function";
						toolCallResult["arguments"] = functionToolCall.Args;
					}

					toolCalls.Add(toolCallResult);
				}
				if (toolCalls.Count > 0)
					result["tool_calls"] = toolCalls;
			}

			return result;
		}

		private JsonObject FromTool(ITool tool)
		{
			var result = new JsonObject
			{
				["name"] = tool.Name,
			};

			if (tool is FunctionTool functionTool)
			{
				result["description"] = functionTool.Description;
				result["arguments_schema"] = functionTool.ArgumentSchema.DeepClone();
			}

			return result;
		}
	}
}
