using LLMDesktopAssistant.LLM.Domain;
using RCLargeLanguageModels.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.LLM.MVVM.Additional.Context;

namespace LLMDesktopAssistant.LLM.Services
{
	public interface IChatSummarizationService
	{
		/// <summary>
		/// Tries to summarize the chat using the usage metadata.
		/// </summary>
		/// <param name="lastUsageMetadata">The usage metadata of the last chat message. This is used to determine if a summary should be generated.</param>
		Task TrySummarizeChatAsync(IUsageMetadata lastUsageMetadata, CancellationToken cancellationToken = default);

		/// <summary>
		/// Summarizes the message with previous messages and summary.
		/// Places summary into this message as <see cref="SummaryViewModel"/>.
		/// </summary>
		/// <param name="message">The message to summarize. This will be updated with the summary if successful.</param>
		/// <returns>True if the summary was successfully generated and placed into the message; otherwise, false.</returns>
		Task SummarizeMessageWithPreviousMessagesAsync(ChatMessage message, CancellationToken cancellationToken = default);
	}
}