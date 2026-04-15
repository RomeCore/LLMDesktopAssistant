namespace LLMDesktopAssistant.Avalonia.DIalogs
{
	[Flags]
	public enum DialogButttons
	{
		None = 0,
		Ok = 1 << 0,
		Cancel = 1 << 1,
	}
}