using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.Prompting.Skills;

namespace LLMDesktopAssistant.Prompting.Context.Providers.Identity
{
	public sealed class IdentitySectionState : PromptSectionStateBase
	{
		public required string? Persona { get; init; }

		public required string? Specialization { get; init; }

		public required string? AssistantNickname { get; init; }
	}
}
