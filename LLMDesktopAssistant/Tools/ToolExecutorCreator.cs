using DocumentFormat.OpenXml.InkML;
using RCLargeLanguageModels.Json;
using RCLargeLanguageModels.Json.Schema;
using RCLargeLanguageModels.Tools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.Tools
{
	public static class ToolExecutorCreator
	{
		public static (
			JsonObject ArgumentSchema,
			Func<JsonNode, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> Executor)

			Create(Delegate executor)
		{
			return Create(executor.Target, executor.Method);
		}

		public static (
			JsonObject ArgumentSchema,
			Func<JsonNode, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> Executor)

			Create(object? target, MethodInfo method)
		{
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			if (target == null && !method.IsStatic)
				throw new ArgumentException("If method target is null, method must be static.", nameof(method));

			var ret = method.ReturnType;
			if (
				ret != typeof(ReactiveToolResult) &&
				ret != typeof(Task<ReactiveToolResult>) &&
				ret != typeof(ToolResult) &&
				ret != typeof(Task<ToolResult>) &&
				ret != typeof(string) &&
				ret != typeof(Task<string>) &&
				ret != typeof(void) &&
				ret != typeof(Task)
			)
				throw new ArgumentException("Return type must be one of:\n" +
					"ReactiveToolResult, Task<ReactiveToolResult>,\n" +
					"ToolResult, Task<ToolResult>,\n" +
					"string, Task<string>,\n" +
					"void or Task.", nameof(method));

			var parameters = method.GetParameters();
			var parameterMappings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			int toolExecutionContextMapping = -1;
			int cancellationTokenMapping = -1;

			var requiredSchemaProperties = new JsonArray();
			var schemaProperties = new JsonObject();
			var argumentSchema = new JsonObject
			{
				["type"] = "object",
				["properties"] = schemaProperties,
				["additionalProperties"] = false
			};

			for (int paramIndex = 0; paramIndex < parameters.Length; paramIndex++)
			{
				var parameter = parameters[paramIndex];

				if (parameter.ParameterType == typeof(ToolExecutionContext))
				{
					if (toolExecutionContextMapping != -1)
						throw new ArgumentException("ToolExecutionContext can only be specified once.", nameof(method));
					toolExecutionContextMapping = paramIndex;
				}
				else if (parameter.ParameterType == typeof(CancellationToken))
				{
					if (cancellationTokenMapping != -1)
						throw new ArgumentException("CancellationToken can only be specified once.", nameof(method));
					cancellationTokenMapping = paramIndex;
				}
				else
				{
					var parameterAccessor = new JsonMemberAccessor(parameter);
					if (!parameterAccessor.Include)
						continue;

					var parameterSchema = JsonSchemaGenerator.Generate(parameterAccessor);
					schemaProperties.Add(parameterAccessor.Name, parameterSchema);
					if (parameterAccessor.Required)
						requiredSchemaProperties.Add(parameterAccessor.Name);

					parameterMappings.Add(parameterAccessor.Name, paramIndex);
				}
			}

			if (requiredSchemaProperties.Count > 0)
				argumentSchema["required"] = requiredSchemaProperties;

			async Task<ReactiveToolResult> Func(JsonNode args, ToolExecutionContext context, CancellationToken cancellationToken)
			{
				var inParams = new object[parameters.Length];
				var objArgs = args as JsonObject ?? [];

				try
				{
					for (int i = 0; i < parameters.Length; i++)
						if (parameters[i].HasDefaultValue)
							inParams[i] = parameters[i].DefaultValue!;

					foreach (var kvp in parameterMappings)
					{
						var arg = objArgs[kvp.Key];
						if (arg == null)
							continue;

						var type = parameters[kvp.Value].ParameterType;
						inParams[kvp.Value] = JsonSerializer.Deserialize(arg, type)!;
					}

					if (toolExecutionContextMapping != -1)
						inParams[toolExecutionContextMapping] = context;
					if (cancellationTokenMapping != -1)
						inParams[cancellationTokenMapping] = cancellationToken;
				}
				catch (Exception ex)
				{
					throw new ArgumentException($"Failed to deserialize arguments: {ex.Message}", nameof(args), ex);
				}

				var value = method.Invoke(target, inParams)!;

				switch (value)
				{
					case Task<ReactiveToolResult> _1:
						return await _1;

					case ReactiveToolResult _2:
						return _2;

					case Task<ToolResult> _3:
						return ReactiveToolResult.CreateFromResult(await _3);

					case ToolResult _4:
						return ReactiveToolResult.CreateFromResult(_4);

					case Task<string> _5:
						return ReactiveToolResult.CreateSuccess(await _5);

					case string _6:
						return ReactiveToolResult.CreateSuccess(_6);

					case Task _7:
						await _7;
						return ReactiveToolResult.CreateSuccess("");

					default: // void or null
						return ReactiveToolResult.CreateSuccess("");
				}
			}

			return (argumentSchema, Func);
		}
	}
}