using RCLargeLanguageModels.Completions;

namespace LLMDesktopAssistant.LLM.Services
{
	public interface ILLMPropertiesBuilder
	{
		/// <summary>
		/// Builds the properties for a given agent ID.
		/// </summary>
		/// <param name="agentId">The agent ID.</param>
		/// <returns>The completion properties.</returns>
		public IEnumerable<CompletionProperty> BuildProperties(Guid agentId);
	}
}