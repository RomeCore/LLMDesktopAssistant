namespace LLMDesktopAssistant.Modules
{
	/// <summary>
	/// The exception that is thrown when a module cannot be found.
	/// </summary>
	public class ModuleNotFoundException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ModuleNotFoundException"/> class with a specified error message.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		public ModuleNotFoundException(string message) : base(message)
		{
		}
	}
}