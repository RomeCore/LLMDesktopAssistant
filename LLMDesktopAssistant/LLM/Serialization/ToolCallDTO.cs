using System.Text.Json.Nodes;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.Serialization
{
	public class ToolCallDTO
	{
		public string Type { get; set; } = string.Empty;
		public string ToolName { get; set; } = string.Empty;
		public string ToolCallId { get; set; } = string.Empty;
		public JsonNode Arguments { get; set; } = JsonValue.Create<string?>(null)!;

		public ToolCallDTO()
		{
		}

		public static ToolCallDTO ConvertFrom(IToolCall toolCall)
		{
			var result = new ToolCallDTO();

			switch (toolCall)
			{
				case FunctionToolCall functionCall:
					result.Type = "function";
					result.ToolName = functionCall.ToolName;
					result.ToolCallId = functionCall.Id;
					result.Arguments = functionCall.Args;
					break;

				default:

					throw new ArgumentException($"Unsupported tool call type: {toolCall.GetType().Name}");
			}

			return result;
		}

		public IToolCall ConvertBack()
		{
			switch (Type)
			{
				case "function":
					return new FunctionToolCall(ToolCallId, ToolName, Arguments);

				default:
					throw new ArgumentException($"Unsupported tool call type: {Type}");
			}
		}
	}
}