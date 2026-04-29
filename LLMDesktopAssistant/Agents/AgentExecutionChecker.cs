using LLMDesktopAssistant.LLM.Domain;
using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// The base class for an execution checker that determines when to execute an agent.
	/// </summary>
	[JsonDerivedType(typeof(AlwaysAgentExecutionChecker), typeDiscriminator: "always")]
	public abstract class AgentExecutionChecker : NotifyPropertyChanged
	{
		/// <summary>
		/// Determines whether the agent should execute based on the current state of the chat.
		/// </summary>
		/// <param name="chat">The chat instance where agent is executed.</param>
		/// <param name="alreadyExecuted">True if the agent has already been executed in this execution cycle.</param>
		/// <returns>True if the agent should execute, false otherwise.</returns>
		public abstract bool ShouldExecute(Chat chat, bool alreadyExecuted);

		/// <summary>
		/// The trigger that always executes the agent.
		/// </summary>
		public static AgentExecutionChecker Always { get; } = new AlwaysAgentExecutionChecker();
	}
}