using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Services;
using LLTSharp;

namespace LLMDesktopAssistant.Tools
{
	[Service(typeof(IToolApprovalService))]
	public class ToolApprovalService : IToolApprovalService
	{
		public (ToolPolicyDecision Decision, string Message) ApproveTool(
			Chat? chat,
			ToolApprovalLevel toolApprovalLevel,
			ToolBehaviour toolExpectedBehaviour,
			ToolBehaviour autoApproveBehaviours,
			ToolBehaviour disallowedBehaviours)
		{
			switch (toolApprovalLevel)
			{
				case ToolApprovalLevel.AlwaysApprove:
					return (ToolPolicyDecision.Approve, string.Empty);

				case ToolApprovalLevel.AlwaysAsk:
					return (ToolPolicyDecision.Ask, string.Empty);

				case ToolApprovalLevel.AlwaysDisallow:
					return (ToolPolicyDecision.Disallow, "The tool execution is disallowed by the agent's settings policy.");

				case ToolApprovalLevel.PolicyBased:
				case ToolApprovalLevel.PolicyApproveOrAsk:
				case ToolApprovalLevel.PolicyAutoApproveUnlessDisallowed:
				case ToolApprovalLevel.PolicyAutoDisallowUnlessApproved:
				case ToolApprovalLevel.PolicyAskOrDisallow:

					bool autoApprove = (autoApproveBehaviours & toolExpectedBehaviour) == toolExpectedBehaviour;
					bool disallow = (disallowedBehaviours & toolExpectedBehaviour) != 0;

					switch (toolApprovalLevel)
					{
						case ToolApprovalLevel.PolicyApproveOrAsk:
							disallow = false;
							break;

						case ToolApprovalLevel.PolicyAutoApproveUnlessDisallowed:
							autoApprove = true;
							break;

						case ToolApprovalLevel.PolicyAutoDisallowUnlessApproved:
							disallow = !autoApprove;
							break;

						case ToolApprovalLevel.PolicyAskOrDisallow:
							autoApprove = false;
							break;
					}

					if (disallow)
						return (ToolPolicyDecision.Disallow, $"The tool execution is disallowed by the agent's settings policy. " +
							$"Policy disallows: {disallowedBehaviours}; the tool expected behaviour: {toolExpectedBehaviour}.");

					if (autoApprove)
						return (ToolPolicyDecision.Approve, string.Empty);
					return (ToolPolicyDecision.Ask, string.Empty);

				default:
					throw new InvalidOperationException($"Invalid approval level for a tool: {toolApprovalLevel}");
			}
		}

		public void MemorizeConsent(Chat? chat, ToolConsentResult consentResult)
		{
			
		}
	}
}
