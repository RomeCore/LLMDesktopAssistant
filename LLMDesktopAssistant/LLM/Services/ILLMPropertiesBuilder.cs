using RCLargeLanguageModels.Completions;

namespace LLMDesktopAssistant.LLM.Services
{
	public interface ILLMPropertiesBuilder
	{
		public IEnumerable<CompletionProperty> BuildProperties();
	}
}