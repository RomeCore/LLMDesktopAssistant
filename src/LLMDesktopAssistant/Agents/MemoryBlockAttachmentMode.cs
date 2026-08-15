namespace LLMDesktopAssistant.Agents
{
	public enum MemoryBlockAttachmentMode
	{
		/// <summary>
		/// Standard mode, allows both reading and writing.
		/// </summary>
		Standard,

		/// <summary>
		/// Read-only mode, only allows reading.
		/// </summary>
		ReadOnly,

		/// <summary>
		/// Write-only mode, only allows writing.
		/// </summary>
		WriteOnly
	}
}