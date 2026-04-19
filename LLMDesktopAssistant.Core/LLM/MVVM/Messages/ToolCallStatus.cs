namespace LLMDesktopAssistant.LLM.Messages
{
	public enum ToolCallStatus
	{
		None,
		UserAsked,
		InProgress,
		Success,
		Cancelled,
		Error,
		NoResult
	}
}