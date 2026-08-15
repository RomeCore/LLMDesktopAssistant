namespace LLMDesktopAssistant.Agents.Tasks
{
	public enum AgentTaskExecutionBehaviour
	{
		/// <summary>
		/// Execute the LLM with tools cyclically until the model stops produce tool calls.
		/// </summary>
		Normal,

		/// <summary>
		/// Execute the LLM with tools only once, stop after first cycle.
		/// </summary>
		ExecuteOnce,

		/// <summary>
		/// Only execute the LLM inference, without executing any tools, and only return the model's response.
		/// </summary>
		OnlyResponse
	}
}
