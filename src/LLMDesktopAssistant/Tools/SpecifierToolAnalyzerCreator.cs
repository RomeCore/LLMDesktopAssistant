using System.Reflection;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools.Specifiers;
using RCLargeLanguageModels.Json.Schema;
using Serilog;

namespace LLMDesktopAssistant.Tools
{
	public static class SpecifierToolAnalyzerCreator
	{
		public static Func<Specifier, JsonNode?, ToolExecutionContext, SpecifierMatchResult> Create(
			Delegate specifierAnalyzer, IDictionary<string, JsonMemberAccessor> parameterMetaInfos)
		{
			ArgumentNullException.ThrowIfNull(specifierAnalyzer);

			return Create(specifierAnalyzer.Target, specifierAnalyzer.Method, parameterMetaInfos);
		}

		public static Func<Specifier, JsonNode?, ToolExecutionContext, SpecifierMatchResult> Create(
			object? target, MethodInfo method, IDictionary<string, JsonMemberAccessor> parameterMetaInfos)
		{
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			if (target == null && !method.IsStatic)
				throw new ArgumentException("If method target is null, method must be static.", nameof(method));

			var ret = method.ReturnType;

			if (ret != typeof(SpecifierMatchResult))
				throw new ArgumentException(
					$"Return type must be {nameof(SpecifierMatchResult)}.",
					nameof(method));

			var parameters = method.GetParameters();
			var parameterMappings = new Dictionary<JsonMemberAccessor, int>();

			int specifierMapping = -1;
			int toolExecutionContextMapping = -1;
			int originalArgsMapping = -1;
			int sharedContextMapping = -1;
			var serviceMappings = new Dictionary<int, Type>();

			for (int paramIndex = 0; paramIndex < parameters.Length; paramIndex++)
			{
				var parameter = parameters[paramIndex];

				if (parameter.ParameterType == typeof(Specifier))
				{
					if (specifierMapping != -1)
						throw new ArgumentException("Specifier can only be specified once.", nameof(method));
					specifierMapping = paramIndex;
				}
				else if (parameter.ParameterType == typeof(ToolExecutionContext))
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
				else if (parameter.IsDefined(typeof(SharedContextAttribute)))
				{
					if (sharedContextMapping != -1)
						throw new ArgumentException("[SharedContext] can only be specified once.", nameof(method));
					sharedContextMapping = paramIndex;
				}
				else if (parameter.IsDefined(typeof(InjectAttribute)))
				{
					serviceMappings[paramIndex] = parameter.ParameterType;
				}
				else
				{
					var parameterAccessor = parameterMetaInfos.TryGetValue(parameter.Name
						?? throw new ArgumentException("Method contains parameter without a name.", nameof(method)), out var oea)
						? oea : new JsonMemberAccessor(parameter);

					if (!parameterAccessor.Include)
						continue;

					parameterMappings.Add(parameterAccessor, paramIndex);
				}
			}

			SpecifierMatchResult Func(Specifier specifier, JsonNode? args, ToolExecutionContext context)
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

					foreach (var (accessor, paramIndex) in parameterMappings)
					{
						if (objArgs.ContainsKey(accessor.Name))
						{
							var arg = objArgs[accessor.Name];
							var type = parameters[paramIndex].ParameterType;
							inParams[paramIndex] = ToolArgsJsonNodeConverter.Convert(arg, type, accessor.Name)!;
						}
						else
						{
							if (accessor.HasDefaultValue)
								inParams[paramIndex] = accessor.DefaultValue;
						}
					}

					if (specifierMapping != -1)
						inParams[specifierMapping] = specifier;
					if (toolExecutionContextMapping != -1)
						inParams[toolExecutionContextMapping] = context;
					if (originalArgsMapping != -1)
						inParams[originalArgsMapping] = args;
					if (sharedContextMapping != -1)
						inParams[sharedContextMapping] = context.SharedContext;
				}
				catch (Exception ex)
				{
					throw new ArgumentException("Failed to deserialize specifier analyzer arguments.", nameof(method), ex);
				}

				var value = method.Invoke(target, inParams)!;
				if (sharedContextMapping != -1)
					context.SharedContext = inParams[sharedContextMapping];

				switch (value)
				{
					case SpecifierMatchResult result:
						return result;

					default: // void or null
						return SpecifierMatchResult.NoMatch;
				}
			}

			return Func;
		}
	}
}
