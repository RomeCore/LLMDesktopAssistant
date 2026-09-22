using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Agents.Settings;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Agents.SubAgents
{
	/// <summary>
	/// Represents information about a sub-agent (an <c>agent</c> addon file), including its name,
	/// description, system prompt body and the resources available to the sub-agent.
	/// The <see cref="AddonBase{Self}.Body"/> of the addon is used as the sub-agent's system prompt.
	/// </summary>
	public class SubAgentInfo : AddonChangedBase<SubAgentInfo, SubAgentChange>
	{
		/// <summary>
		/// The list of tools that would be used for this sub-agent without approval.
		/// Examples: 'Read', 'Bash(git:*)'.
		/// </summary>
		public ImmutableList<ToolNameWithSpecifier> AllowedTools
		{
			get;
			set => SetProperty(ref field, value);
		} = [];

		/// <summary>
		/// The list of tools that would be used for this sub-agent.
		/// Also used for setting tool approval policy to ask for these tools.
		/// Examples: 'Read', 'Bash(git:*)'.
		/// </summary>
		public ImmutableList<ToolNameWithSpecifier> AvailableTools
		{
			get;
			set => SetProperty(ref field, value);
		} = [];

		/// <summary>
		/// The list of tools that would be disallowed for this sub-agent.
		/// Examples: 'Read', 'Bash(git:*)'.
		/// </summary>
		public ImmutableList<ToolNameWithSpecifier> DisallowedTools
		{
			get;
			set => SetProperty(ref field, value);
		} = [];

		/// <summary>
		/// The list of skill names that the sub-agent can use.
		/// </summary>
		public ImmutableList<string> Skills
		{
			get;
			set => SetProperty(ref field, value);
		} = [];

		/// <summary>
		/// The list of inner sub-agent names that the sub-agent can use.
		/// </summary>
		public ImmutableList<string> SubAgents
		{
			get;
			set => SetProperty(ref field, value);
		} = [];

		/// <summary>
		/// The list of memory blocks that the sub-agent can use.
		/// </summary>
		public ImmutableDictionary<string, MemoryBlockAttachmentMode> MemoryBlocks
		{
			get;
			set => SetProperty(ref field, value);
		} = [];

		/// <summary>
		/// The model used for this sub-agent. Can be overriden in the sub-agent configuration.
		/// </summary>
		public string? Model
		{
			get;
			set => SetProperty(ref field, value);
		} = null;
	}
}
