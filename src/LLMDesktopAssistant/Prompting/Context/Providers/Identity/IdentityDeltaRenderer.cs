using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.Prompting.Context.Providers.Identity
{
	/// <summary>
	/// Delta renderer of the core prompt section (stub: no deltas yet).
	/// </summary>
	[ChatService(typeof(IPromptSectionDeltaRenderer<IdentitySectionDelta>))]
	public class IdentityDeltaRenderer : IPromptSectionDeltaRenderer<IdentitySectionDelta>
	{
		/// <inheritdoc/>
		public string Render(IdentitySectionDelta delta) => string.Empty;
	}
}
