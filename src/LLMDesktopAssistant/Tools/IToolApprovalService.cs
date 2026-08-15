using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.Tools
{
	public interface IToolApprovalService
	{
		(ToolPolicyDecision Decision, string Message) ApproveTool(
			Chat? chat,
			ToolApprovalLevel toolApprovalLevel,
			ToolBehaviour toolExpectedBehaviour,
			ToolBehaviour autoApproveBehaviours,
			ToolBehaviour disallowedBehaviours);

		void MemorizeConsent(Chat? chat, ToolConsentResult consentResult);
	}
}
