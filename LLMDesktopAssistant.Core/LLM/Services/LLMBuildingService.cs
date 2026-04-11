using AngleSharp.Common;
using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.Modules;
using LLMDesktopAssistant.Core.ToolModules;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;
using System.Collections.Immutable;

namespace LLMDesktopAssistant.Core.LLM.Services
{
	public class LLMBuildingService(
		Chat chat
	) : ILLMBuildingService
	{
		public LLMInfo? BuildChatLLM()
		{
			var model = chat.Settings.ChatModel.Current;
			if (model is null)
				return null;

			return new LLMInfo
			{
				LLM = new LLModel(model),
				ContextSize = model.ContextLength != -1 ? model.ContextLength : 128000
			};
		}

		public LLMInfo? BuildSummarizationLLM()
		{
			var model = chat.Settings.SummarizerModel.Current;
			if (model is null)
				return null;

			return new LLMInfo
			{
				LLM = new LLModel(model),
				ContextSize = model.ContextLength != -1 ? model.ContextLength : 128000
			};
		}
	}
}