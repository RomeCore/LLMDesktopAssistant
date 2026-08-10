using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Json;
using RCLargeLanguageModels.Json.Schema;

namespace LLMDesktopAssistant.Agents.Tasks
{
	public class DelegateAgentTool : AgentTool
	{
		private static readonly JsonSchemaMethodGenerator _schemaGenerator = new();

		private readonly object? _methodTarget;
		private readonly MethodInfo _method;
		private readonly ParameterInfo[] _parameters;
		private readonly ImmutableDictionary<string, int> _parameterMappings;
		private readonly int _ctMapping = -1;

		public override string Name { get; }

		public override string DisplayName { get; }

		public override string Description { get; }

		public override JsonObject ArgumentSchema { get; }

		public DelegateAgentTool(string name, string? displayName, string description, Delegate executor)
			: this(name, displayName, description, executor.Target, executor.Method)
		{
		}

		public DelegateAgentTool(string name, string? displayName, string description, object? methodTarget, MethodInfo method)
		{
			ArgumentNullException.ThrowIfNull(method);
			if (methodTarget == null && !method.IsStatic)
				throw new ArgumentException("If method target is null, method must be static.", nameof(method));

			var ret = method.ReturnType;
			if (
				ret != typeof(AgentToolCallResult) &&
				ret != typeof(Task<AgentToolCallResult>)
				)
				throw new ArgumentException("Return type must be AgentToolCallResult or Task<AgentToolCallResult>.", nameof(method));

			var methodAccessor = new JsonMemberAccessor(method);
			var schemaProperties = new JsonSchemaGeneratorProperties();
			var schema = _schemaGenerator.GenerateSchema(methodAccessor, schemaProperties)!.AsObject();
			var mappingsBuilder = ImmutableDictionary.CreateBuilder<string, int>();

			var parameters = method.GetParameters();
			foreach (var param in parameters)
			{
				if (param.ParameterType == typeof(CancellationToken))
				{
					if (_ctMapping != -1)
						throw new ArgumentException("Multiple parameters of type CancellationToken are not supported.", nameof(method));
					_ctMapping = param.Position;
				}
				else
				{
					var argName = param.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
						?? param.Name!;
					mappingsBuilder.Add(argName, param.Position);
				}
			}

			_methodTarget = methodTarget;
			_method = method;
			_parameters = parameters;
			_parameterMappings = mappingsBuilder.ToImmutable();

			Name = name;
			DisplayName = displayName ?? name;
			Description = description;
			ArgumentSchema = schema;
		}

		public override Task<AgentToolCallPreResult> PreExecuteAsync(JsonNode? arguments, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new AgentToolCallPreResult { ExpectedBehaviour = ToolBehaviour.None });
		}

		public override async Task<AgentToolCallResult> ExecuteAsync(JsonNode? arguments, object? sharedContext, CancellationToken cancellationToken = default)
		{
			var objArgs = arguments as JsonObject ?? [];
			var inParams = new object[_parameters.Length];

			try
			{
				for (int i = 0; i < _parameters.Length; i++)
					if (_parameters[i].HasDefaultValue)
						inParams[i] = _parameters[i].DefaultValue!;

				foreach (var kvp in _parameterMappings)
				{
					var arg = objArgs[kvp.Key];
					if (arg == null)
						continue;

					var type = _parameters[kvp.Value].ParameterType;
					inParams[kvp.Value] = JsonSerializer.Deserialize(arg, type)!;
				}

				if (_ctMapping != -1)
					inParams[_ctMapping] = cancellationToken;
			}
			catch (Exception ex)
			{
				return new AgentToolCallResult
				{
					Success = false,
					Content = $"Failed to deserialize arguments: {ex.Message}"
				};
			}

			try
			{
				var result = _method.Invoke(_methodTarget, inParams);
				return result switch
				{
					AgentToolCallResult callResult => callResult,
					Task<AgentToolCallResult> task => await task,
					_ => new AgentToolCallResult
					{
						Success = true,
						Content = "Tool was executed successfully."
					}
				};
			}
			catch (Exception ex)
			{
				return new AgentToolCallResult
				{
					Success = false,
					Content = $"Error executing tool: {ex.Message}"
				};
			}
		}
	}
}
