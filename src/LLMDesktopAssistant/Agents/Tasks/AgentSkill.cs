namespace LLMDesktopAssistant.Agents.Tasks
{
	public abstract class AgentSkill
	{
		/// <summary>
		/// The unique name of the skill.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// A brief description of what the skill does.
		/// </summary>
		public abstract string Description { get; }

		/// <summary>
		/// The path to the SKILL.md file, if applicable.
		/// </summary>
		public abstract string? Path { get; }

		/// <summary>
		/// The home directory of the skill, if applicable.
		/// </summary>
		public abstract string? HomeDirectory { get; }

		/// <summary>
		/// Gets the body of the skill, which is typically the contents of a SKILL.md file.
		/// </summary>
		/// <returns>The body of the skill.</returns>
		public abstract Task<string> GetBodyAsync(CancellationToken cancellationToken = default);
	}
}
