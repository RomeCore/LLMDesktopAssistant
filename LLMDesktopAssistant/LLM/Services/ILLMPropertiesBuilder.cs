using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Settings;
using RCLargeLanguageModels.Completions;

namespace LLMDesktopAssistant.LLM.Services
{
	public interface ILLMPropertiesBuilder
	{
		/// <summary>
		/// Builds the properties for a given agent descriptor and returns the completion properties.
		/// </summary>
		/// <param name="agent">The agent.</param>
		/// <param name="chatSettings">The chat settings used to resolve inherited generation settings.</param>
		/// <returns>The completion properties.</returns>
		public IEnumerable<CompletionProperty> BuildProperties(ChatAgentDescriptor agent, ChatSettings chatSettings);
	}
}
