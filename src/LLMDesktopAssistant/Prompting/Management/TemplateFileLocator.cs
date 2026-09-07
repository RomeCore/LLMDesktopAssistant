using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.Services;
using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Management
{
	/// <summary>
	/// The locator that finds template addons in addon packs and folders.
	/// </summary>
	[Service(typeof(IAddonFileLocator<ITemplate>))]
	public class TemplateFileLocator(ITemplateParser parser) : AddonFileLocatorBase<ITemplate>
	{
		protected override string[] Folders => ["templates"];

		protected override string[] Extensions => parser.SupportedExtensions;

		protected override bool UseNameDeduplication => false;

		protected override bool AllowShortFormat => true;

		protected override string? FullFormatName => null;
	}
}
