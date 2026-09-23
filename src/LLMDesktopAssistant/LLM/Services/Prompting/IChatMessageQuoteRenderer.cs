using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// Renders a neutral content quote of a chat message: name + time + content + attachment metadata.
	/// No reasoning, no tool calls and no reader perspective (visibility/exposure) involved.
	/// </summary>
	public interface IChatMessageQuoteRenderer
	{
		/// <summary>
		/// Renders the quote of the given message.
		/// </summary>
		/// <param name="message">The branched message to render.</param>
		string RenderQuote(BranchedMessage message);
	}
}
