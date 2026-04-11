using LLMDesktopAssistant.Core.LLM.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.LLM.Services
{
	/// <summary>
	/// Defines the contract for managing chat sessions, including creating, retrieving, and cleaning up chats, as well as
	/// providing scoped access to chat-related services.
	/// </summary>
	public interface IChatManagementService
	{
		/// <summary>
		/// Retrieves a collection of chat information objects representing all available chats.
		/// </summary>
		/// <returns>An enumerable collection of <see cref="ChatInfo"/> objects. The collection is empty if no chats are available.</returns>
		IEnumerable<ChatInfo> GetChats();

		/// <summary>
		/// Opens a new service scope for the given chat information.
		/// This services scope will contain the <see cref="Chat"/> instance and any other related dependencies.
		/// </summary>
		/// <param name="chatId">The ID of the chat to open a scope for.</param>
		/// <returns>A service scope containing the chat and related dependencies.</returns>
		IServiceScope OpenChatScope(int chatId);

		/// <summary>
		/// Removes all chat sessions that do not contain any messages.
		/// </summary>
		/// <remarks>Use this method to clean up unused or placeholder chat sessions. This operation does not affect
		/// chats that contain at least one message.</remarks>
		void ClearEmptyAndTemporaryChats();

		/// <summary>
		/// Creates a new chat with the specified title.
		/// </summary>
		/// <param name="title">The title of the chat to create. Cannot be null or empty.</param>
		/// <returns>A <see cref="ChatInfo"/> object containing details of the newly created chat.</returns>
		ChatInfo CreateChat(string title);
	}
}