using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	public interface IPromptDumpService
	{
		void Dump(IEnumerable<IMessage> messages, IEnumerable<ITool> tools);
	}
}