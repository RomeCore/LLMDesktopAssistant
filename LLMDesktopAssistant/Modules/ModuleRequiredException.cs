
namespace LLMDesktopAssistant.Modules
{
	[Serializable]
	internal class ModuleRequiredException : Exception
	{
		public ModuleRequiredException()
		{
		}

		public ModuleRequiredException(string? message) : base(message)
		{
		}

		public ModuleRequiredException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}