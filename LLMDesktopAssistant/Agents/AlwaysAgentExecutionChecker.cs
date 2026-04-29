using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// A <see cref="AgentExecutionChecker"/> that always executes the agent.
	/// </summary>
	public class AlwaysAgentExecutionChecker : AgentExecutionChecker
	{
		public override bool ShouldExecute(Chat chat, bool alreadyExecuted)
		{
			return true;
		}
	}
}