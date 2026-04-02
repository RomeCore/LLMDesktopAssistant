using AngleSharp.Common;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.ToolModules;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;
using System.Collections.Immutable;

namespace LLMDesktopAssistant.LLM.Services
{
	public class LLMBuildingService(
		Chat chat,
		IToolsetBuildingService toolsetBuilder
		) : ILLMBuildingService
	{
		public LLMInfo BuildChatLLM()
		{
			var model = chat.Settings.ChatModel.Current ?? throw new Exception("Model is not set.");

			return new LLMInfo
			{
				LLM = new LLModel(model),
				Tools = toolsetBuilder.BuildTools().ToImmutableDictionary(t => t.Tool.Name),
				ContextSize = model.ContextLength != -1 ? model.ContextLength : 160000
			};
		}

		public LLMInfo BuildSummarizationLLM()
		{
			var model = chat.Settings.SummarizerModel.Current ?? throw new Exception("Summarizer model is not set.");

			return new LLMInfo
			{
				LLM = new LLModel(model),
				ContextSize = model.ContextLength != -1 ? model.ContextLength : 160000
			};
		}
	}
}