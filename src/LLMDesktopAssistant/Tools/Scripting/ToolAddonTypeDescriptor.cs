using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Tools.Scripting
{
	[AddonTypeDescriptor]
	public class ToolAddonTypeDescriptor : IAddonTypeDescriptor
	{
		public string Type => "tools";
		public Type ClrType => typeof(ToolInfo);
		public bool UseDefaultDiagnosticFactory => true;
		public LocaleKeyBase NameKey => Locale.GetKey("addon.type.tools.name");
		public LocaleKeyBase? DescriptionKey => Locale.GetKey("addon.type.tools.description");
	}
}
