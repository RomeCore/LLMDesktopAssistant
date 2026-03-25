using System.Text.Json.Nodes;
using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Interface for converting assistant and tool response messages to <see cref="Domain.AssistantMessage"/>.
	/// </summary>
	public interface IMessageConverter
	{
		/// <summary>
		/// Converts a domain chat message to a collection of <see cref="IMessage"/>.
		/// </summary>
		/// <param name="message">The domain chat message to convert.</param>
		/// <returns>The converted message.</returns>
		IEnumerable<IMessage> Convert(Domain.ChatMessage message);
	}
}