using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Prompting.Skills
{
	/// <summary>
	/// The locator that finds skill addons (<c>SKILL.md</c>/<c>SKILL.mdx</c>) in addon packs and folders.
	/// </summary>
	[Service(typeof(IAddonFileLocator<SkillInfo>))]
	public class SkillFileLocator : AddonFileLocatorBase<SkillInfo>
	{
		public override string[] Folders => ["skills"];

		public override string[] Extensions => [".md", ".mdx"];

		public override bool AllowShortFormat => true;

		public override string? FullFormatName => "SKILL";
	}
}
