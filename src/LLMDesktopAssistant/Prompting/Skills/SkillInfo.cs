using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.StructuredValues.Const;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Prompting.Skills
{
	/// <summary>
	/// Represents information about a skill (an <c>SKILL.md</c> addon), including its name, description, and source.
	/// </summary>
	public class SkillInfo : AddonChangedBase<SkillInfo, SkillChange>
	{
		/// <summary>
		/// The list of tools that would be used in this skill without approval.
		/// Used for UI display for helping user understand behaviour of the skill.
		/// Examples: 'Read', 'Bash(git:*)'.
		/// TODO (optional): Add support for dynamic tool loading when skill activates.
		/// </summary>
		public ImmutableList<ToolNameWithSpecifier> AllowedTools
		{
			get;
			set => SetProperty(ref field, value);
		} = [];

		/// <summary>
		/// The list of tools that would be loaded when skill activates.
		/// Used for UI display for helping user understand behaviour of the skill.
		/// Examples: 'Read', 'Bash(git:*)'.
		/// TODO (optional): Add support for dynamic tool loading when skill activates.
		/// </summary>
		public ImmutableList<ToolNameWithSpecifier> AvailableTools
		{
			get;
			set => SetProperty(ref field, value);
		} = [];

		/// <summary>
		/// The list of tools that would be disallowed when skill activates.
		/// Used for UI display for helping user understand behaviour of the skill.
		/// Examples: 'Read', 'Bash(git:*)'.
		/// TODO (optional): Add support for dynamic tool loading when skill activates.
		/// </summary>
		public ImmutableList<ToolNameWithSpecifier> DisallowedTools
		{
			get;
			set => SetProperty(ref field, value);
		} = [];

		/// <summary>
		/// The mode in which the skill should be injected into the prompt.
		/// </summary>
		public SkillInjectionMode InjectionMode
		{
			get;
			set => SetProperty(ref field, value);
		} = SkillInjectionMode.Default;

	}
}
