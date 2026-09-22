using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Prompting
{
	/// <summary>
	/// Represents a tool definition for LLM providers (then providers injects it into the system prompt typically).
	/// This is not executable at all.
	/// </summary>
	public readonly struct SerializableToolDefinition : IEquatable<SerializableToolDefinition>
	{
		public required string Name { get; init; }

		public required string Description { get; init; }

		public required string ArgumentSchema { get; init; }

		public static SerializableToolDefinition From(ToolInfo tool)
		{
			return new SerializableToolDefinition
			{
				Name = tool.Name,
				Description = tool.Description,
				ArgumentSchema = tool.ArgumentSchema.ToJsonString()
			};
		}

		public static SerializableToolDefinition From(FunctionTool tool)
		{
			return new SerializableToolDefinition
			{
				Name = tool.Name,
				Description = tool.Description,
				ArgumentSchema = tool.ArgumentSchema.ToJsonString()
			};
		}

		public FunctionTool ToFunctionTool()
		{
			var argumentSchema = JsonNode.Parse(ArgumentSchema) as JsonObject ?? throw new InvalidOperationException("JSON schema must be a JSON object.");
			return new FunctionTool(Name, Description, argumentSchema, (a, ct) => throw new NotImplementedException("This tool is not indented to be invoked."));
		}

		public bool Equals(SerializableToolDefinition other)
		{
			return Name == other.Name && Description == other.Description && ArgumentSchema == other.ArgumentSchema;
		}

		public override bool Equals([NotNullWhen(true)] object? obj)
		{
			return obj is SerializableToolDefinition other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, Description, ArgumentSchema);
		}

		public static bool operator ==(SerializableToolDefinition left, SerializableToolDefinition right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(SerializableToolDefinition left, SerializableToolDefinition right)
		{
			return !(left == right);
		}
	}
}
