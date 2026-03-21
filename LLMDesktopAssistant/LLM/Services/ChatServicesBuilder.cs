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
			this.AddSingleton<Chat>();
		}
	}

	/// <summary>
	/// Interface for chat execution service.
	/// </summary>
	public interface IChatExecutionService
	{
		/// <summary>
		/// Generates a response to the provided user message asynchronously.
		/// </summary>
		/// <param name="userMessage">The user message to process.</param>
		/// <param name="cancellationToken">Token for cancellation of the operation.</param>
		/// <returns>A task representing the asynchronous operation that will produce a response message.</returns>
		Task<AssistantMessage> GenerateResponseAsync(UserMessage userMessage, CancellationToken cancellationToken = default);
	}


}