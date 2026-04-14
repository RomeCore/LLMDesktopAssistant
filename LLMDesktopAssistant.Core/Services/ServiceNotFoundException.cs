namespace LLMDesktopAssistant.Core.Services
{
	/// <summary>
	/// The exception that is thrown when a module cannot be found.
	/// </summary>
	public class ServiceNotFoundException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceNotFoundException"/> class with a specified error message.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		public ServiceNotFoundException(string message) : base(message)
		{
		}
	}
}