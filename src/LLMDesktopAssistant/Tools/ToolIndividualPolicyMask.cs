namespace LLMDesktopAssistant.Tools
{
	public readonly struct ToolIndividualPolicyMask
	{
		public required ToolBehaviour AllowedBehaviours { get; init; }

		public required ToolBehaviour DisallowedBehaviours { get; init; }
	}
}