using LLMDesktopAssistant.Agents;
using RCLargeLanguageModels.Completions;

namespace LLMDesktopAssistant.LLM.Services
{
	public interface ILLMPropertiesBuilder
	{
		/// <summary>
		/// Builds the properties for a given agent descriptor and returns the completion properties.
		/// </summary>
		/// <param name="agent">The agent.</param>
		/// <returns>The completion properties.</returns>
		public IEnumerable<CompletionProperty> BuildProperties(ChatAgentDescriptor agent);
	}
}