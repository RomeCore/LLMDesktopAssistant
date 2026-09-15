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
		public override string[] Folders => ["templates"];

		public override string[] Extensions => parser.SupportedExtensions;

		public override bool AllowShortFormat => true;

		public override string? FullFormatName => null;

		protected override bool UseNameDeduplication => false;
	}
}
