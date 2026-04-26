using AngleSharp.Common;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;
using System.Collections.Immutable;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(ILLMBuildingService))]
	public class LLMBuildingService(
		Chat chat
	) : ILLMBuildingService
	{
		public LLMInfo? BuildChatLLM()
		{
			var model = chat.Settings.Models.ChatModel.Current;
			if (model is null)
				return null;

			return new LLMInfo
			{
				LLM = new LLModel(model)
			};
		}

		public LLMInfo? BuildSummarizationLLM()
		{
			var model = chat.Settings.Summarization.SummarizerModel.Current;
			if (model is null)
				return null;

			return new LLMInfo
			{
				LLM = new LLModel(model)
			};
		}
	}
}