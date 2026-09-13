using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Agents.SubAgents
{
	/// <summary>
	/// The locator that finds sub-agent addons (<c>agent.md</c>/<c>AGENT.md</c>) in addon packs and folders.
	/// </summary>
	[Service(typeof(IAddonFileLocator<SubAgentInfo>))]
	public class SubAgentFileLocator : AddonFileLocatorBase<SubAgentInfo>
	{
		protected override string[] Folders => ["agents"];

		protected override string[] Extensions => [".md", ".mdx"];

		protected override bool AllowShortFormat => true;

		protected override string? FullFormatName => "AGENT";
	}
}
