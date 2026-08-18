namespace LLMDesktopAssistant.Agents.SubAgents
{
	/// <summary>
	/// Represents information about a sub-agent file, including its path and source.
	/// </summary>
	/// <param name="FileName">The full path to the sub-agent file.</param>
	/// <param name="Source">The source of the sub-agent.</param>
	public readonly record struct SubAgentFileInfo(string FileName, SubAgentSource Source);
}
