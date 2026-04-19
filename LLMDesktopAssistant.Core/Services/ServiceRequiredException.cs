
namespace LLMDesktopAssistant.Services
{
	[Serializable]
	internal class ServiceRequiredException : Exception
	{
		public ServiceRequiredException()
		{
		}

		public ServiceRequiredException(string? message) : base(message)
		{
		}

		public ServiceRequiredException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}