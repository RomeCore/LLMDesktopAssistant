namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Interface for a service that reveals hidden messages in a chat.
	/// </summary>
	public interface IMessageRevealService
	{
		/// <summary>
		/// Reveals all messages in the chat.
		/// </summary>
		void RevealMessages();
	}
}