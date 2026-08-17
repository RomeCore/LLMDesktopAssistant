using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.Tools.Consents
{
	public interface IToolApprovalService
	{
		(ToolPolicyDecision Decision, string Message) ApproveTool(
			ToolApprovalLevel toolApprovalLevel,
			ToolBehaviour toolExpectedBehaviour,
			ToolBehaviour autoApproveBehaviours,
			ToolBehaviour disallowedBehaviours);
	}
}
