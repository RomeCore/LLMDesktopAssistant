using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Tools
{
	public static class ToolArgsJsonNodeConverter
	{
		private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
		{
			NumberHandling = JsonNumberHandling.AllowReadingFromString,
			PropertyNameCaseInsensitive = true,
		};

		public static object? Convert(JsonNode? node, Type targetType)
		{
			return node.Deserialize(targetType, _jsonSerializerOptions);
		}
	}
}
