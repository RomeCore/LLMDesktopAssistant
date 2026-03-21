namespace LLMDesktopAssistant.LLM.Data
{
	/// <summary>
	/// The exception that is thrown when the data in the conversation database is corrupted.
	/// </summary>
	public class InvalidDataException : Exception
	{
		public InvalidDataException(string message) : base(message)
		{
		}

		public InvalidDataException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}