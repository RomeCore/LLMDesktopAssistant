using LLMDesktopAssistant.Core.LLM.Domain;
using RCLargeLanguageModels.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.LLM.Services
{
	public interface IChatSummarizationService
	{
		/// <summary>
		/// Tries to summarize the chat using the specified LLM and usage metadata.
		/// </summary>
		/// <param name="usedLLM">The LLM that was used to generate the latest chat message.</param>
		/// <param name="lastUsageMetadata">The usage metadata of the last chat message. This is used to determine if a summary should be generated.</param>
		Task TrySummarizeChat(LLMInfo usedLLM, IUsageMetadata lastUsageMetadata);
	}
}