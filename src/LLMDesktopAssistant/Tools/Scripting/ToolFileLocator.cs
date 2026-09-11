using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Tools.Scripting
{
	[Service(typeof(IAddonFileLocator<ToolInfo>))]
	public class ToolFileLocator : AddonFileLocatorBase<ToolInfo>
	{
		private readonly IScriptableToolEngine[] _engines;

		public ToolFileLocator(IEnumerable<IScriptableToolEngine> engines) => _engines = [.. engines];

		protected override string[] Folders => ["tools"];

		protected override string[] Extensions => field ??= [.. _engines.SelectMany(e => e.Descriptor.Extensions).Distinct()];
		protected override bool AllowShortFormat => true;
		protected override string? FullFormatName => null;
	}
}
