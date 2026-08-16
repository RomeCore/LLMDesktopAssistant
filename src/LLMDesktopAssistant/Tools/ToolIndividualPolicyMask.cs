namespace LLMDesktopAssistant.Tools
{
	public readonly struct ToolIndividualPolicyMask
	{
		public required ToolBehaviour AutoApproveBehaviours { get; init; }

		public required ToolBehaviour DisallowedBehaviours { get; init; }
	}
}