namespace LLMDesktopAssistant.LLM.MVVM.Messages
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