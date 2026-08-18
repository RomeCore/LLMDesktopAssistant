namespace LLMDesktopAssistant.Agents.SubAgents
{
	/// <summary>
	/// Parses sub-agent definitions from file contents.
	/// </summary>
	public interface ISubAgentParser
	{
		/// <summary>
		/// Parses the specified sub-agent file contents into a <see cref="SubAgentInfo"/>.
		/// </summary>
		/// <param name="fullpath">The full path to the sub-agent file.</param>
		/// <param name="contents">The contents of the sub-agent file.</param>
		/// <param name="source">The source of the sub-agent.</param>
		/// <returns>The parsed <see cref="SubAgentInfo"/>.</returns>
		SubAgentInfo Parse(string fullpath, string contents, SubAgentSource source = SubAgentSource.Unknown);
	}
}
