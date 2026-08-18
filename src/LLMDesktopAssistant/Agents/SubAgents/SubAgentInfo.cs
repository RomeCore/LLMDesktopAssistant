using LLMDesktopAssistant.Tools;
using YamlDotNet.RepresentationModel;

namespace LLMDesktopAssistant.Agents.SubAgents
{
	public class SubAgentInfo
	{
		/// <summary>
		/// The name of the sub-agent. Used for identification and display purposes.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// The description of the sub-agent. Used to understand the purpose of the sub-agent and when it should be used.
		/// </summary>
		public required string Description { get; init; }

		/// <summary>
		/// The sub-agent file content getter, excluding the YAML frontmatter.
		/// </summary>
		public required Func<string> BodyGetter { get; init; }

		/// <summary>
		/// The source of the sub-agent.
		/// </summary>
		public required SubAgentSource Source { get; init; }

		/// <summary>
		/// The absolute path to the sub-agent file, if applicable. Null otherwise.
		/// </summary>
		public string? Path { get; init; } = null;

		/// <summary>
		/// The home directory for this sub-agent. Null if the sub-agent does not have a home directory.
		/// </summary>
		public string? HomeDirectory { get; init; } = null;

		/// <summary>
		/// The metadata associated with the sub-agent.
		/// This dictionary can be used to store additional information about the sub-agent, such as its version number or author.
		/// </summary>
		public ImmutableDictionary<SubAgentMetadataType, string> Metadata { get; init; } = [];

		/// <summary>
		/// The additional metadata associated with the sub-agent.
		/// Used for metadata values that are not covered by <see cref="SubAgentMetadataType"/>.
		/// </summary>
		public ImmutableDictionary<string, string> AdditionalMetadata { get; init; } = [];

		/// <summary>
		/// The list of tools that would be used for this sub-agent without approval.
		/// Examples: 'Read', 'Bash(git:*)'.
		/// </summary>
		public ImmutableList<ToolNameWithSpecifier> AllowedTools { get; init; } = [];

		/// <summary>
		/// The list of tools that would be used for this sub-agent.
		/// Also used for setting tool approval policy to ask for these tools.
		/// Examples: 'Read', 'Bash(git:*)'.
		/// </summary>
		public ImmutableList<ToolNameWithSpecifier> AvailableTools { get; init; } = [];

		/// <summary>
		/// The list of tools that would be disallowed for this sub-agent.
		/// Examples: 'Read', 'Bash(git:*)'.
		/// </summary>
		public ImmutableList<ToolNameWithSpecifier> DisallowedTools { get; init; } = [];

		/// <summary>
		/// The tags associated with the sub-agent. Used for UI display and search.
		/// Examples: 'development', 'code-quality', 'refactoring'.
		/// </summary>
		public ImmutableList<string> Tags { get; init; } = [];

		/// <summary>
		/// The additional properties associated with the sub-agent.
		/// Used for root properties that are not covered by other properties of this class.
		/// </summary>
		public ImmutableDictionary<string, YamlNode> AdditionalProperties { get; init; } = [];

		/// <summary>
		/// The diagnostic containing specific warnings and errors that was occured during sub-agent parsing.
		/// </summary>
		public SubAgentDiagnostic? Diagnostic { get; init; } = null;

		/// <summary>
		/// Whether or not this sub-agent is enabled. Defaults to true.
		/// </summary>
		public bool Enabled { get; init; } = true;

		/// <summary>
		/// The model used for this sub-agent. Can be overriden in the sub-agent configuration.
		/// </summary>
		public string? Model { get; init; } = null;

		/// <summary>
		/// Gets the list of overriden sub-agents during deduplication by name.
		/// </summary>
		public ImmutableList<SubAgentInfo> Overrides { get; init; } = [];
	}
}
