namespace LLMDesktopAssistant.Core.LLM.MVVM.Messages
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