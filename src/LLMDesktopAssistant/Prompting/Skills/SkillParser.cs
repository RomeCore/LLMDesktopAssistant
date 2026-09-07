using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Parsers;
using LLMDesktopAssistant.Addons.Parsers.Frontmatter;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Prompting.Skills
{
	/// <summary>
	/// The parser for <c>SKILL.md</c> / <c>SKILL.mdx</c> addon files.
	/// </summary>
	[Service(typeof(IAddonFileParser<SkillInfo>))]
	public class SkillParser : FrontmatterBasedAddonParser<SkillInfo>
	{
		protected override AddonParserDescriptor GetDescriptorFor(string content, AddonPathInfo fileInfo)
		{
			return new AddonParserDescriptor
			{
				FrontmatterStart = "---",
				FrontmatterEnd = "---",
				RequiresFrontmatter = false,
				UseMarkdownFallback = true
			};
		}

		protected override void Populate(SkillInfo addon, AddonFrontmatterDocument frontmatter, ref AddonDiagnostic? diagnostic)
		{
			addon.AllowedTools = frontmatter.Get<ImmutableList<ToolNameWithSpecifier>>("allowed-tools", []);
			addon.AvailableTools = frontmatter.Get<ImmutableList<ToolNameWithSpecifier>>("available-tools", []);
			addon.DisallowedTools = frontmatter.Get<ImmutableList<ToolNameWithSpecifier>>("disallowed-tools", []);

			var injectionMode = frontmatter.Get<string>("injection-mode");
			if (!string.IsNullOrWhiteSpace(injectionMode))
			{
				switch (injectionMode.Trim().ToLowerInvariant())
				{
					case "default":
						addon.InjectionMode = SkillInjectionMode.Default;
						break;

					case "full":
						addon.InjectionMode = SkillInjectionMode.Full;
						break;

					default:
						diagnostic = diagnostic.Combine(new AddonDiagnostic
						{
							Messages = [$"Unknown injection mode '{injectionMode}'. Expected 'default' or 'full'."]
						});
						break;
				}
			}
		}

		protected override bool IsValidName(string name)
		{
			return SkillName.IsValidSkillName(name);
		}

		protected override string NormalizeName(string name)
		{
			return SkillName.ToValidSkillName(name);
		}

		protected override bool IsNameMatchingFile(string name, AddonPathInfo fileInfo)
		{
			return name.Equals(GetFallbackName(fileInfo), StringComparison.Ordinal);
		}
	}
}
