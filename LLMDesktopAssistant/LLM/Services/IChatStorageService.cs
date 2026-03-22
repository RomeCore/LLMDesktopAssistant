using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Interface for chat storage service. This service is responsible for storing and retrieving chat data.
	/// </summary>
	public interface IChatStorageService
	{
		/// <summary>
		/// Loads chat data (settings and messages) into the <see cref="Chat"/>.
		/// </summary>
		void Load();

		/// <summary>
		/// Appends message to the storage and <see cref="Chat"/>.
		/// </summary>
		/// <param name="message"></param>
		void AppendMessage(ChatMessage message);

		/// <summary>
		/// Switches branch at the specified message index.
		/// </summary>
		/// <param name="messageIndex"></param>
		/// <param name="newBranchIndex"></param>
		void SwitchBranch(int messageIndex, int newBranchIndex);

		/// <summary>
		/// Edits message in the specified message index.
		/// </summary>
		/// <param name="editIndex"></param>
		/// <param name="newMessage"></param>
		void EditMessage(int editIndex, ChatMessage newMessage);
	}
}