using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Providers;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels;
using Serilog;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(ILLMBuildingService))]
	public class LLMBuildingService(
		Chat chat,
		IAgentManagementService agentSettings,
		IModelManager modelManager
	) : ILLMBuildingService
	{
		public LLMInfo? BuildChatLLM(Guid agentId)
		{
			var settings = agentSettings.GetAgentDescriptor(agentId).Generation;

			var modelName = settings.EnableCustomModel && !string.IsNullOrEmpty(settings.Model)
				? settings.Model
				: chat.Settings.Models.ChatModel;

			if (string.IsNullOrEmpty(modelName))
				return null;

			try
			{
				var llm = modelManager.GetModel(modelName);
				return new LLMInfo { LLM = llm };
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "Failed to build chat LLM for model {ModelName}", modelName);
				return null;
			}
		}

		public LLMInfo? BuildSummarizationLLM()
		{
			var modelName = chat.Settings.Summarization.SummarizerModel;
			if (string.IsNullOrEmpty(modelName))
				return null;

			try
			{
				var llm = modelManager.GetModel(modelName);
				return new LLMInfo { LLM = llm };
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "Failed to build summarization LLM for model {ModelName}", modelName);
				return null;
			}
		}
	}
}
