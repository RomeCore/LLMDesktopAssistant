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
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
			PropertyNameCaseInsensitive = true,
		};

		public static object? Convert(JsonNode? node, Type targetType, string? argName)
		{
			try
			{
				return node.Deserialize(targetType, _jsonSerializerOptions);
			}
			catch (Exception ex)
			{
				throw new ArgumentException($"Failed to convert argument '{node?.ToJsonString(_jsonSerializerOptions) ?? "null"}' (name '{argName ?? "unknown"}') to type {targetType.FullName}: {ex.Message}", argName, ex);
			}
		}
	}
}
