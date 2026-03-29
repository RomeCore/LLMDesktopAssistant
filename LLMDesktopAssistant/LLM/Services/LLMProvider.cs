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
	public class LLMProvider(Chat chat) : ILLMProvider
	{
		public LLMInfo GetChatLLM()
		{
			var toolModules = ModuleManager.GetAll<ToolModule>();

			var tools = toolModules
				.Concat(chat.AdditionalToolModules ?? [])
				.Where(m => m != null && m.Enabled)
				.SelectMany(m => m.GetTools())
				.ToImmutableDictionary(t => t.Tool.Name);

			return new LLMInfo
			{
				LLM = new LLModel(deepseek_chat),
				Tools = tools,
				ContextSize = 160000
			};
		}

		public LLMInfo GetSummarizationLLM()
		{
			return new LLMInfo
			{
				LLM = new LLModel(deepseek, "deepseek-chat"),
				ContextSize = 160000
			};
		}
	}
}