using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Prompting.Skills
{
	/// <summary>
	/// The addon type descriptor that registers the skill addon type ('skills') in the addon system.
	/// </summary>
	[Service(typeof(IAddonTypeDescriptor))]
	public class SkillAddonTypeDescriptor : IAddonTypeDescriptor
	{
		public string Type => "skills";

		public Type ClrType => typeof(SkillInfo);
	}
}
