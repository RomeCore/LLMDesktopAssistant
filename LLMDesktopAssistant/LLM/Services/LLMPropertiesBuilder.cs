using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Settings;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Completions.Properties;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(ILLMPropertiesBuilder))]
	public class LLMPropertiesBuilder(
		Chat chat
	) : ILLMPropertiesBuilder
	{
		public IEnumerable<CompletionProperty> BuildProperties()
		{
			var result = new List<CompletionProperty>();
			var properties = chat.Settings.LLMProperties;

			if (properties.EnableTemperature)
			{
				result.Add(new TemperatureProperty(properties.Temperature / 2.0f));
			}

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

			foreach (var parameter in properties.AdditionalParameters)
			{
				if (!parameter.Enabled)
					continue;

				var node = JsonSerializer.SerializeToNode(parameter.ParameterValue) ?? JsonValue.Create((string?)null)!;
				result.Add(new CustomProperty(parameter.ParameterName, node));
			}

			return result;
		}
	}
}