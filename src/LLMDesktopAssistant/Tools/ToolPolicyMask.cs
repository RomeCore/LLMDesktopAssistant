namespace LLMDesktopAssistant.Tools
{
	public readonly struct ToolPolicyMask
	{
		public required ToolBehaviour AutoApproveBehaviours { get; init; }

		public required ToolBehaviour DisallowedBehaviours { get; init; }
	}
}