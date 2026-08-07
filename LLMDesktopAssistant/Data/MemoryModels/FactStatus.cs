namespace LLMDesktopAssistant.Data.MemoryModels
{
	public enum FactStatus
	{
		/// <summary>
		/// The fact is currently active and relevant.
		/// </summary>
		Active,

		/// <summary>
		/// The fact has been superseded by a newer, more accurate fact.
		/// </summary>
		Superseded,

		/// <summary>
		/// The fact has been explicitly marked as no longer relevant or useful.
		/// </summary>
		Deleted
	}
}
