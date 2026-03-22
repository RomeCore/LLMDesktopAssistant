using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.LLM.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Builder for chat services. This builder is used to configure and build the services required for the chat functionality.
	/// </summary>
	public class ChatServicesBuilder : ServiceCollection
	{
		public ChatServicesBuilder()
		{
			this.AddScoped<Chat>();
		}
	}

	/// <summary>
	/// Interface for chat storage service. This service is responsible for storing and retrieving chat data.
	/// </summary>
	public interface IChatStorageService
	{
		/// <summary>
		/// Initializes the storage service asynchronously.
		/// This method should load chat data (settings and messages) into the <see cref="Chat"/>.
		/// </summary>
		/// <param name="cancellationToken">Token for cancellation of the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task InitializeAsync(CancellationToken cancellationToken = default);

		void AppendMessage(ChatMessage message);
	}
}