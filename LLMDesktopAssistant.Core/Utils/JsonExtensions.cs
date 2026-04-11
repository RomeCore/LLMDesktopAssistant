using System.Text.Json;
using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.Core.Utils
{
	public static class JsonExtensions
	{
		public static JsonNode? ToNodeSafe(this JsonElement element)
		{
			return element.ValueKind switch
			{
				JsonValueKind.Object => JsonObject.Create(element),
				JsonValueKind.Array => JsonArray.Create(element),
				JsonValueKind.String => JsonValue.Create(element),
				_ => throw new InvalidOperationException($"Unsupported JSON value kind: {element.ValueKind}")
			};
		}

		public static JsonNode ToNode(this JsonElement element)
		{
			return ToNodeSafe(element) ??
				throw new InvalidOperationException("Failed to convert JsonElement to JsonNode.");
		}

		public static string? ToJsonString(this JsonElement element,
			JsonSerializerOptions? serializerOptions = null)
		{
			return ToNodeSafe(element)?.ToJsonString(serializerOptions);
		}
	}
}