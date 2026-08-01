using LLMDesktopAssistant.Prompting.Skills;

namespace LLMDesktopAssistant.Agents.Tasks
{
	/// <summary>
	/// The adapter for a chat skill. This is used to adapt the <see cref="SkillInfo"/> descriptor into an <see cref="AgentSkill"/> object.
	/// </summary>
	public class ChatAgentSkill : AgentSkill
	{
		/// <summary>
		/// The <see cref="SkillInfo"/> descriptor that is being adapted.
		/// </summary>
		public required SkillInfo ChatSkillInfo { get; init; }

		public override string Name => ChatSkillInfo.Name;
		public override string Description => ChatSkillInfo.Description;
		public override string? Path => ChatSkillInfo.Path;
		public override string? HomeDirectory => ChatSkillInfo.HomeDirectory;
		public override Task<string> GetBodyAsync(CancellationToken cancellationToken = default) => Task.FromResult(ChatSkillInfo.BodyGetter());
	}
}
