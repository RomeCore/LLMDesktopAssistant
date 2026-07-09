using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Tools.Implementations;
using LLMDesktopAssistant.Agents.ExecutionStages;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Settings related to language models used in chat.
	/// </summary>
	public class ChatModelSettings : ChatSettingsCategoryBase
	{
		private string _chatModel = string.Empty;
		/// <summary>
		/// The model to use for chat. Format: "ProviderName$ModelName".
		/// </summary>
		public string ChatModel
		{
			get => _chatModel;
			set => SetProperty(ref _chatModel, value);
		}

		private string _agenticToolsModel = string.Empty;
		/// <summary>
		/// The model to use for <see cref="AgenticToolModule"/>.
		/// Format: "ProviderName$ModelName".
		/// </summary>
		public string AgenticToolsModel
		{
			get => _agenticToolsModel;
			set => SetProperty(ref _agenticToolsModel, value);
		}

		private string _routerModel = string.Empty;
		/// <summary>
		/// The model to use for agentic routing in the <see cref="AdaptiveAgentExecutionStage"/>.
		/// Format: "ProviderName$ModelName".
		/// </summary>
		public string AgenticRouterModel
		{
			get => _routerModel;
			set => SetProperty(ref _routerModel, value);
		}

		private string _visionModel = string.Empty;
		/// <summary>
		/// The model to use for vision and image-understanding tasks.
		/// Format: "ProviderName$ModelName".
		/// </summary>
		public string VisionModel
		{
			get => _visionModel;
			set => SetProperty(ref _visionModel, value);
		}
	}
}
