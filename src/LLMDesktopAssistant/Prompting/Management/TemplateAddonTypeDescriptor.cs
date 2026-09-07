using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Localization;
using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Management
{
	[AddonTypeDescriptor]
	public class TemplateAddonTypeDescriptor : IAddonTypeDescriptor
	{
		public string Type => "templates";

		public Type ClrType => typeof(ITemplate);

		public bool UseDefaultDiagnosticFactory => false;

		public LocaleKeyBase NameKey => Locale.GetKey("addon.type.templates.name");

		public LocaleKeyBase? DescriptionKey => Locale.GetKey("addon.type.templates.description");
	}
}
