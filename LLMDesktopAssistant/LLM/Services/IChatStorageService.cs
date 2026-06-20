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
		void Reload();

		/// <summary>
		/// Appends message to the storage and the <see cref="Chat"/>.
		/// </summary>
		/// <param name="message"></param>
		void AppendMessage(ChatMessage message);

		/// <summary>
		/// Places a new branch at the specified message index. The previous message becomes a leaf of the current chat.
		/// </summary>
		/// <param name="messageIndex"></param>
		void PlaceNewBranch(int messageIndex);

		/// <summary>
		/// Switches branch at the specified message index.
		/// </summary>
		/// <param name="messageIndex"></param>
		/// <param name="newBranchIndex"></param>
		void SwitchBranch(int messageIndex, int newBranchIndex);

		/// <summary>
		/// Edits message into the specified message index.
		/// </summary>
		/// <param name="messageIndex"></param>
		/// <param name="newMessage"></param>
		void EditMessage(int messageIndex, ChatMessage newMessage);

		/// <summary>
		/// Deletes message with all descendants at the specified message index and switches to sibling branch.
		/// </summary>
		/// <param name="messageIndex"></param>
		void DeleteMessageWithDescendants(int messageIndex);
	}
}