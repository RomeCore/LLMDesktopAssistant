using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Prompting.Skills
{
	/// <summary>
	/// The locator that finds skill addons (<c>SKILL.md</c>/<c>SKILL.mdx</c>) in addon packs and folders.
	/// </summary>
	[Service(typeof(IAddonFileLocator<SkillInfo>))]
	public class SkillFileLocator : AddonFileLocatorBase<SkillInfo>
	{
		protected override string[] Folders => ["skills"];

		protected override string[] Extensions => [".md", ".mdx"];

		protected override bool AllowShortFormat => false;

		protected override string? FullFormatName => "SKILL";
	}
}
