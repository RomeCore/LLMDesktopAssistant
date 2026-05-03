using LLMDesktopAssistant.LLM.Settings;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Completions.Properties;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(ILLMPropertiesBuilder))]
	public class LLMPropertiesBuilder(
		IAgentManagementService agentSettings
	) : ILLMPropertiesBuilder
	{
		public IEnumerable<CompletionProperty> BuildProperties(Guid agentId)
		{
			var result = new List<CompletionProperty>();
			var properties = agentSettings.GetAgentDescriptor(agentId).Generation;

			if (properties.EnableReasoningSettings)
			{
				if (properties.ReasoningSettings == ReasoningSettings.Disabled)
					result.Add(new ReasoningProperty(false));

				else if (properties.ReasoningSettings != ReasoningSettings.Default)
					result.Add(new ReasoningProperty(properties.ReasoningSettings switch
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

			if (properties.EnableTemperature)
			{
				result.Add(new TemperatureProperty(properties.Temperature / 2.0f));
			}

			if (properties.EnableMaxTokens)
			{
				result.Add(new MaxTokensProperty(properties.MaxTokens));
			}

			foreach (var parameter in properties.AdditionalParameters)
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