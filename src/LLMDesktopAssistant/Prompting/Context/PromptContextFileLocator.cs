using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Prompting.Context
{
	/// <summary>
	/// The locator that finds scriptable prompt context addons in addon packs and folders.
	/// </summary>
	[Service(typeof(IAddonFileLocator<PromptContextInfo>))]
	public class PromptContextFileLocator : AddonFileLocatorBase<PromptContextInfo>
	{
		public override string[] Folders => ["context"];

		public override string[] Extensions => []; // TODO: Implement

		public override bool AllowShortFormat => true;

		public override string? FullFormatName => null;
	}
}
