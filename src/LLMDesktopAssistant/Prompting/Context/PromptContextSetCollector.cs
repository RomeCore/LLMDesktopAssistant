using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Prompting.Skills;

namespace LLMDesktopAssistant.Prompting.Context
{
	[ChatService(typeof(IAddonSetCollector<PromptContextInfo>))]
	public class PromptContextSetCollector(
		IEnumerable<PromptContextNativeProvider> nativeProviders,
		IChatSettingsService chatSettings,
		IServiceProvider services
	) : AddonSetCollectorBase<PromptContextInfo, PromptContextChange>(services)
	{
		protected override bool AdditionalGoingFirst => false;
		protected override bool AdditionalOverrides => true;

		protected override IEnumerable<PromptContextInfo> GetAdditionalAddons()
		{
			return nativeProviders.SelectMany(p => p.GetContexts());
		}

		public override IEnumerable<PromptContextInfo> GetAddonsForAgent(ChatAgentDescriptor agent)
		{
			var settings = agent.Context;
			var skillset = settings.GetEffectiveContextSet(chatSettings.Settings);
			return GetAddonsWithChanges(skillset, agent);
		}
	}
}
