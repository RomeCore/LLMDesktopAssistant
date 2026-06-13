using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Services;
using RCLargeLanguageModels.Json.Schema;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// Creates pre-execution functions from delegates, similar to <see cref="ToolExecutorCreator"/>
	/// but without argument schema generation.
	/// </summary>
	public static class PreviewToolExecutorCreator
	{
		/// <summary>
		/// Creates a pre-executor function from the specified delegate.
		/// </summary>
		/// <param name="preExecutor">The delegate to create a pre-executor from. Can be null.</param>
		/// <returns>A function that takes JSON arguments, context, and cancellation token, and returns a pre-execution result. Null if the input delegate is null.</returns>
		public static Func<JsonNode, ToolExecutionContext, CancellationToken, Task<PreviewToolExecutionResult>> Create(Delegate preExecutor)
		{
			if (preExecutor == null)
				throw new ArgumentNullException(nameof(preExecutor));

			return Create(preExecutor.Target, preExecutor.Method);
		}

		/// <summary>
		/// Creates a pre-executor function from the specified target and method.
		/// </summary>
		/// <param name="target">The target object on which the method is invoked. Null for static methods.</param>
		/// <param name="method">The method to invoke.</param>
		/// <returns>A function that takes JSON arguments, context, and cancellation token, and returns a pre-execution result.</returns>
		public static Func<JsonNode, ToolExecutionContext, CancellationToken, Task<PreviewToolExecutionResult>> Create(object? target, MethodInfo method)
		{
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			if (target == null && !method.IsStatic)
				throw new ArgumentException("If method target is null, method must be static.", nameof(method));

			var ret = method.ReturnType;

			if (ret != typeof(PreviewToolExecutionResult) && ret != typeof(Task<PreviewToolExecutionResult>))
				throw new ArgumentException(
					$"Return type must be {nameof(PreviewToolExecutionResult)} or Task<{nameof(PreviewToolExecutionResult)}>.",
					nameof(method));

			var parameters = method.GetParameters();
			var parameterMappings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			int toolExecutionContextMapping = -1;
			int originalArgsMapping = -1;
			int sharedContextMapping = -1;
			int cancellationTokenMapping = -1;
			var serviceMappings = new Dictionary<int, Type>();

			for (int paramIndex = 0; paramIndex < parameters.Length; paramIndex++)
			{
				var parameter = parameters[paramIndex];

				if (parameter.ParameterType == typeof(ToolExecutionContext))
				{
					if (toolExecutionContextMapping != -1)
						throw new ArgumentException("ToolExecutionContext can only be specified once.", nameof(method));
					toolExecutionContextMapping = paramIndex;
				}
				else if (parameter.ParameterType.IsAssignableTo(typeof(JsonNode)) && parameter.IsDefined(typeof(OriginalArgsAttribute)))
				{
					if (originalArgsMapping != -1)
						throw new ArgumentException("[OriginalArgs] JsonNode can only be specified once.", nameof(method));
					originalArgsMapping = paramIndex;
				}
				else if (parameter.ParameterType.IsByRef && parameter.IsDefined(typeof(SharedContextAttribute)))
				{
					if (sharedContextMapping != -1)
						throw new ArgumentException("[SharedContext] @ref can only be specified once.", nameof(method));
					sharedContextMapping = paramIndex;
				}
				else if (parameter.ParameterType == typeof(CancellationToken))
				{
					if (cancellationTokenMapping != -1)
						throw new ArgumentException("CancellationToken can only be specified once.", nameof(method));
					cancellationTokenMapping = paramIndex;
				}
				else if (parameter.IsDefined(typeof(InjectAttribute)))
				{
					serviceMappings[paramIndex] = parameter.ParameterType;
				}
				else
				{
					var parameterAccessor = new JsonMemberAccessor(parameter);
					if (!parameterAccessor.Include)
						continue;

					parameterMappings.Add(parameterAccessor.Name, paramIndex);
				}
			}

			async Task<PreviewToolExecutionResult> Func(JsonNode args, ToolExecutionContext context, CancellationToken cancellationToken)
			{
				var inParams = new object?[parameters.Length];
				var objArgs = args as JsonObject ?? [];

				try
				{
					for (int i = 0; i < parameters.Length; i++)
						if (parameters[i].HasDefaultValue)
							inParams[i] = parameters[i].DefaultValue!;

					foreach (var (i, serviceType) in serviceMappings)
						inParams[i] = context.Chat.Services.GetService(serviceType);

					foreach (var (name, paramIndex) in parameterMappings)
					{
						var arg = objArgs[name];
						if (arg == null)
							continue;

						var type = parameters[paramIndex].ParameterType;
						inParams[paramIndex] = ToolArgsJsonNodeConverter.Convert(arg, type, name);
					}

					if (toolExecutionContextMapping != -1)
						inParams[toolExecutionContextMapping] = context;
					if (originalArgsMapping != -1)
						inParams[originalArgsMapping] = args;
					if (sharedContextMapping != -1)
						inParams[sharedContextMapping] = context.SharedContext;
					if (cancellationTokenMapping != -1)
						inParams[cancellationTokenMapping] = cancellationToken;
				}
				catch (Exception ex)
				{
					throw new ArgumentException($"Failed to deserialize pre-executor arguments: {ex.Message}", nameof(args), ex);
				}

				var value = method.Invoke(target, inParams)!;
				if (sharedContextMapping != -1)
					context.SharedContext = inParams[sharedContextMapping];

				switch (value)
				{
				case Task<PreviewToolExecutionResult> asyncResult:
					return await asyncResult;

				case PreviewToolExecutionResult syncResult:
					return syncResult;

				default: // void or null
					return new PreviewToolExecutionResult();
				};
			}

			return Func;
		}
	}
}
