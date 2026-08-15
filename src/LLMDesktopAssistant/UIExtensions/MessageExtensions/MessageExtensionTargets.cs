namespace LLMDesktopAssistant.UIExtensions.MessageExtensions
{
	/// <summary>
	/// The targets of the extension, which determines where it can be instantiated.
	/// </summary>
	[Flags]
	public enum MessageExtensionTargets
	{
		/// <summary>
		/// Extension will not be instantiated anywhere.
		/// </summary>
		None = 0,

		/// <summary>
		/// Extension will be instantiated in the user's message.
		/// </summary>
		User = 1,

		/// <summary>
		/// Extension will be instantiated in the assistant's message.
		/// </summary>
		Assistant = 2,

		/// <summary>
		/// Extension will be instantiated in both the user's and assistant's messages.
		/// </summary>
		Both = User | Assistant
	}
}
