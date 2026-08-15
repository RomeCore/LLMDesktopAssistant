namespace LLMDesktopAssistant.Agents.Tasks
{
	public enum AgentToolCallStatus
	{
		Pending,

		PreExecuting,

		Confirming,

		Executing,

		Failed,

		Success,

		Cancelled
	}
}
