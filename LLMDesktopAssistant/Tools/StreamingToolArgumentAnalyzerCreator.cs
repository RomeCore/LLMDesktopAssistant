using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Services;
using RCLargeLanguageModels.Json.Schema;
using Serilog;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// Creates streaming pre-execution functions from delegates, similar to <see cref="ToolExecutorCreator"/>
	/// but without argument schema generation.
	/// </summary>
	public static class StreamingToolArgumentAnalyzerCreator
	{
		/// <summary>
		/// Creates a pre-executor function from the specified delegate.
		/// </summary>
		/// <param name="preExecutor">The delegate to create a pre-executor from. Can be null.</param>
		/// <returns>A function that takes JSON arguments, context, and cancellation token, and returns a pre-execution result. Null if the input delegate is null.</returns>
		public static Func<JsonNode, ToolExecutionContext, StreamingToolArgumentsAnalysisResult> Create(Delegate preExecutor)
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
		public static Func<JsonNode, ToolExecutionContext, StreamingToolArgumentsAnalysisResult> Create(object? target, MethodInfo method)
		{
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			if (target == null && !method.IsStatic)
				throw new ArgumentException("If method target is null, method must be static.", nameof(method));

			var ret = method.ReturnType;

			if (ret != typeof(StreamingToolArgumentsAnalysisResult))
				throw new ArgumentException(
					$"Return type must be {nameof(StreamingToolArgumentsAnalysisResult)}.",
					nameof(method));

			var parameters = method.GetParameters();
			var parameterMappings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			int toolExecutionContextMapping = -1;
			int originalArgsMapping = -1;
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

			StreamingToolArgumentsAnalysisResult Func(JsonNode args, ToolExecutionContext context)
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
						inParams[paramIndex] = ToolArgsJsonNodeConverter.Convert(arg, type);
					}

					if (toolExecutionContextMapping != -1)
						inParams[toolExecutionContextMapping] = context;
					if (originalArgsMapping != -1)
						inParams[originalArgsMapping] = args;
				}
				catch (Exception ex)
				{
					Log.Debug(ex, "Failed to deserialize arguments for streaming pre-execution function: {Args}", args);
					return new StreamingToolArgumentsAnalysisResult();
				}

				var value = method.Invoke(target, inParams)!;

				switch (value)
				{
					case StreamingToolArgumentsAnalysisResult result:
						return result;

					default: // void or null
						return new StreamingToolArgumentsAnalysisResult();
				}
				;
			}

			return Func;
		}
	}
}
