using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Agents.SubAgents
{
	/// <summary>
	/// The addon type descriptor that registers the sub-agent addon type ('agents') in the addon system.
	/// </summary>
	[AddonTypeDescriptor]
	public class SubAgentAddonTypeDescriptor : IAddonTypeDescriptor
	{
		public string Type => "agents";

		public Type ClrType => typeof(SubAgentInfo);

		public bool UseDefaultDiagnosticFactory => true;

		public bool UseDefaultSearchService => true;

		public LocaleKeyBase NameKey => Locale.GetKey("addon.type.agents.name");

		public LocaleKeyBase? DescriptionKey => Locale.GetKey("addon.type.agents.description");
	}
}
