namespace LLMDesktopAssistant.Tools;

/// <summary>
/// Extension methods for <see cref="ToolApprovalLevel"/>.
/// </summary>
public static class ToolApprovalLevelExtensions
{
	/// <summary>
	/// Returns <see langword="true"/> when the approval level defers the decision to the
	/// agent's behaviour-based policy settings; <see langword="false"/> for the
	/// <c>Always*</c> levels that bypass the policy entirely.
	/// </summary>
	/// <param name="approvalLevel">The approval level to check.</param>
	/// <returns><see langword="true"/> for <see cref="ToolApprovalLevel.PolicyBased"/>,
	/// <see cref="ToolApprovalLevel.PolicyAskOrDisallow"/>, <see cref="ToolApprovalLevel.PolicyApproveOrAsk"/>,
	/// <see cref="ToolApprovalLevel.PolicyAutoApproveUnlessDisallowed"/> and
	/// <see cref="ToolApprovalLevel.PolicyAutoDisallowUnlessApproved"/>; otherwise <see langword="false"/>.</returns>
	public static bool IsPolicyBased(this ToolApprovalLevel approvalLevel)
	{
		return approvalLevel is ToolApprovalLevel.PolicyBased or
			ToolApprovalLevel.PolicyAskOrDisallow or
			ToolApprovalLevel.PolicyApproveOrAsk or
			ToolApprovalLevel.PolicyAutoApproveUnlessDisallowed or
			ToolApprovalLevel.PolicyAutoDisallowUnlessApproved;
	}
}
