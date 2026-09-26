namespace LLMDesktopAssistant.Prompting.Context.Providers.Identity
{
	/// <summary>
	/// The core prompt section (main part of the system prompt).
	/// </summary>
	public class IdentitySection(IServiceProvider services)
		: PromptAnchoredSectionBase<IdentitySectionState, IdentitySectionDelta>(services)
	{
		public override string Discriminator => "identity";
	}
}
