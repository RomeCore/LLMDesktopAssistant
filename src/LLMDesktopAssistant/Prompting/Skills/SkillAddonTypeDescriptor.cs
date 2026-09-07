using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Prompting.Skills
{
	/// <summary>
	/// The addon type descriptor that registers the skill addon type ('skills') in the addon system.
	/// </summary>
	[AddonTypeDescriptor]
	public class SkillAddonTypeDescriptor : IAddonTypeDescriptor
	{
		public string Type => "skills";

		public Type ClrType => typeof(SkillInfo);

		public LocaleKeyBase NameKey => Locale.GetKey("addon.type.skills.name");

		public LocaleKeyBase? DescriptionKey => Locale.GetKey("addon.type.skills.description");
	}
}
