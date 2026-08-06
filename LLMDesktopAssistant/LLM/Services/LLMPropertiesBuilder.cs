using System.Text.Json.Nodes;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Settings;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Completions.Properties;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(ILLMPropertiesBuilder))]
	public class LLMPropertiesBuilder() : ILLMPropertiesBuilder
	{
		/// <inheritdoc/>
		public IEnumerable<CompletionProperty> BuildProperties(ChatAgentDescriptor agent, ChatSettings chatSettings)
		{
			var result = new List<CompletionProperty>();

			var reasoning = agent.Generation.GetEffectiveReasoning(chatSettings);
			if (reasoning.EnableReasoningSettings)
			{
				if (reasoning.ReasoningSettings == ReasoningSettings.Disabled)
					result.Add(new ReasoningProperty(false));

				else if (reasoning.ReasoningSettings != ReasoningSettings.Default)
					result.Add(new ReasoningProperty(reasoning.ReasoningSettings switch
					{
						ReasoningSettings.None => ReasoningEffort.None,
						ReasoningSettings.Minimal => ReasoningEffort.Minimal,
						ReasoningSettings.Low => ReasoningEffort.Low,
						ReasoningSettings.Medium => ReasoningEffort.Medium,
						ReasoningSettings.High => ReasoningEffort.High,
						ReasoningSettings.XHigh => ReasoningEffort.XHigh,
						ReasoningSettings.Maximum => ReasoningEffort.Max,
						_ => ReasoningEffort.Medium
					}));
			}

			var temperature = agent.Generation.GetEffectiveTemperature(chatSettings);
			if (temperature.EnableTemperature)
			{
				result.Add(new TemperatureProperty(temperature.Temperature / 2.0f));
			}

			var maxTokens = agent.Generation.GetEffectiveMaxTokens(chatSettings);
			if (maxTokens.EnableMaxTokens)
			{
				result.Add(new MaxTokensProperty(maxTokens.MaxTokens));
			}

			foreach (var parameter in agent.Generation.GetEffectiveAdditionalParameters(chatSettings))
			{
				if (!parameter.Enabled)
					continue;

				var node = JsonNode.Parse(parameter.ParameterValue) ?? JsonValue.Create((string?)null)!;
				result.Add(new CustomProperty(parameter.ParameterName, node));
			}

			return result;
		}
	}
}
