using AngleSharp.Common;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;
using System.Collections.Immutable;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(ILLMBuildingService))]
	public class LLMBuildingService(
		Chat chat,
		IAgentManagementService agentSettings
	) : ILLMBuildingService
	{
		public LLMInfo? BuildChatLLM(Guid agentId)
		{
			var settings = agentSettings.GetAgentDescriptor(agentId).Generation;

			var descriptor = settings.EnableCustomModel
				? settings.Model
				: chat.Settings.Models.ChatModel;

			var model = descriptor.Current;
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