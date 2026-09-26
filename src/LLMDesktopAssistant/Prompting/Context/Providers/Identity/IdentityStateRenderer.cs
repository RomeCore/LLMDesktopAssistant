using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.Prompting.Context.Providers.Identity
{
	/// <summary>
	/// Renders the core prompt section state into a snapshot (text only).
	/// </summary>
	[ChatService(typeof(IPromptSectionStateRenderer<IdentitySectionState>))]
	public class IdentityStateRenderer(
		ITemplateLibraryAccessor templates
	) : IPromptSectionStateRenderer<IdentitySectionState>
	{
		/// <inheritdoc/>
		public SystemPromptSnapshot Render(IdentitySectionState state)
		{
			return templates.GetTextTemplate("identity_system_section").Render(new
			{
				persona = state.Persona,
				specialization = state.Specialization,
				assistant_nickname = state.AssistantNickname
			});
		}
	}
}
