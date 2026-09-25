using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Parsers;
using LLMDesktopAssistant.Addons.Parsers.Frontmatter;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Prompting.Context
{
	[Service(typeof(IAddonFileParser<PromptContextInfo>))]
	public class PromptContextParser : FrontmatterBasedAddonParser<PromptContextInfo>
	{
		protected override AddonParserDescriptor GetDescriptorFor(string content, AddonPathInfo fileInfo)
		{
			return new AddonParserDescriptor
			{
				FrontmatterStart = "---",
				FrontmatterEnd = "---"
			};
		}

		protected override void Populate(PromptContextInfo addon, AddonFrontmatterDocument frontmatter, ref AddonDiagnostic? diagnostic)
		{
			
		}
	}
}
