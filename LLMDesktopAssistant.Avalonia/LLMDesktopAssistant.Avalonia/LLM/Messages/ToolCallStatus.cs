namespace LLMDesktopAssistant.Avalonia.LLM.Messages
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