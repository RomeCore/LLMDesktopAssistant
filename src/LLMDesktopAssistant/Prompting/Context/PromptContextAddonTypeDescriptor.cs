using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Prompting.Context
{
	/// <summary>
	/// The addon type descriptor that registers the prompt context addon type.
	/// </summary>
	[AddonTypeDescriptor]
	public class PromptContextAddonTypeDescriptor : IAddonTypeDescriptor
	{
		public string Type => "context";

		public AddonKind Kind => AddonKind.PromptContext;

		public Type ClrType => typeof(PromptContextInfo);

		public bool UseDefaultDiagnosticFactory => true;

		public bool UseDefaultSearchService => true;

		public LocaleKeyBase NameKey => Locale.GetKey("addon.type.context.name");

		public LocaleKeyBase? DescriptionKey => Locale.GetKey("addon.type.context.description");
	}
}
