using LLMDesktopAssistant.Agents.SubAgents;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// The kind of a broken sub-agent dependency.
/// </summary>
public enum SubAgentLinkIssueKind
{
	/// <summary>
	/// A referenced skill does not exist.
	/// </summary>
	Skill,

	/// <summary>
	/// A referenced sub-agent does not exist.
	/// </summary>
	SubAgent,

	/// <summary>
	/// A referenced memory block does not exist.
	/// </summary>
	MemoryBlock
}

/// <summary>
/// Describes a broken dependency of a sub-agent: a referenced skill, sub-agent or memory block
/// that is not available in the current session.
/// </summary>
public sealed record SubAgentLinkIssue(SubAgentLinkIssueKind Kind, string Name);

/// <summary>
/// Validates the dependencies of a <see cref="SubAgentInfo"/> against the sets of available
/// skills, sub-agents and memory blocks, producing a list of broken references.
/// </summary>
public static class SubAgentLinkChecker
{
	/// <summary>
	/// Checks the dependencies of the specified sub-agent against the available sets.
	/// </summary>
	/// <param name="info">The sub-agent to check.</param>
	/// <param name="skillNames">The names of all available skills.</param>
	/// <param name="subAgentNames">The names of all available sub-agents.</param>
	/// <param name="memoryBlockNames">The names of all available memory blocks.</param>
	/// <returns>The list of broken references.</returns>
	public static ImmutableList<SubAgentLinkIssue> Check(SubAgentInfo info,
		HashSet<string> skillNames, HashSet<string> subAgentNames, HashSet<string> memoryBlockNames)
	{
		var issues = ImmutableList.CreateBuilder<SubAgentLinkIssue>();

		foreach (var skill in info.Skills)
		{
			if (!skillNames.Contains(skill))
				issues.Add(new SubAgentLinkIssue(SubAgentLinkIssueKind.Skill, skill));
		}

		foreach (var subAgent in info.SubAgents)
		{
			if (!subAgentNames.Contains(subAgent))
				issues.Add(new SubAgentLinkIssue(SubAgentLinkIssueKind.SubAgent, subAgent));
		}

		foreach (var blockName in info.MemoryBlocks.Keys)
		{
			if (!memoryBlockNames.Contains(blockName))
				issues.Add(new SubAgentLinkIssue(SubAgentLinkIssueKind.MemoryBlock, blockName));
		}

		return issues.ToImmutable();
	}
}
