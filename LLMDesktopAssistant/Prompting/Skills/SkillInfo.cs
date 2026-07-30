using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace LLMDesktopAssistant.Prompting.Skills
{
	public class SkillInfo
	{
		/// <summary>
		/// The unique name of the skill.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// The description of the skill.
		/// </summary>
		public required string Description { get; init; }

		/// <summary>
		/// The SKILL.md content, excluding the YAML frontmatter.
		/// </summary>
		public required string Body { get; set; }

		/// <summary>
		/// The absolute path to the SKILL.md file, if applicable. Null otherwise.
		/// </summary>
		public required string? Path { get; init; }

		/// <summary>
		/// The home directory for this skill. Null if the skill does not have a home directory.
		/// </summary>
		public required string? HomeDirectory { get; init; }

		/// <summary>
		/// The metadata associated with the skill.
		/// This dictionary can be used to store additional information about the skill, such as its version number or author.
		/// </summary>
		public required ImmutableDictionary<SkillMetadataType, string> Metadata { get; init; }

		/// <summary>
		/// The additional metadata associated with the skill.
		/// Used for metadata values that are not covered by <see cref="SkillMetadataType"/>.
		/// </summary>
		public required ImmutableDictionary<string, string> AdditionalMetadata { get; init; }

		/// <summary>
		/// The fuzzy list of tools that would be used in this skill.
		/// Used for UI display for helping user understand behaviour of the skill.
		/// Examples: 'Read', 'Bash(git:*)'
		/// </summary>
		public required ImmutableList<string> AllowedTools { get; init; }

		/// <summary>
		/// The tags associated with the skill. Used for UI display and search.
		/// Examples: 'development', 'code-quality', 'refactoring'.
		/// </summary>
		public required ImmutableList<string> Tags { get; init; }

		/// <summary>
		/// The additional properties associated with the skill.
		/// Used for root properties that are not covered by other properties of this class.
		/// </summary>
		public required ImmutableDictionary<string, YamlNode> AdditionalProperties { get; init; }

		/// <summary>
		/// Whether or not this skill is enabled. Defaults to true.
		/// </summary>
		public bool Enabled { get; init; } = true;

		/// <summary>
		/// The mode in which the skill should be injected into the prompt.
		/// </summary>
		public SkillInjectionMode InjectionMode { get; init; } = SkillInjectionMode.Default;
	}
}
