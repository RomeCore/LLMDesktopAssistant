using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Prompting.Context;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// 
	/// </summary>
	public interface IPromptSupersedeContextProcessor
	{
		void Process(ChatAgentDescriptor agent,
			EffectiveChatContext effectiveContext, IEnumerable<IPromptSupersedeContextProvider> providers);
	}
}
