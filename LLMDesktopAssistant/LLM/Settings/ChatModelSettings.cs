using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Tools.Implementations;
using LLMDesktopAssistant.Agents.ExecutionStages;
using RCLargeLanguageModels.Clients;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Settings related to language models used in chat.
	/// </summary>
	public class ChatModelSettings : ChatSettingsCategoryBase
	{
		private LLModelDescriptorTracked _chatModel = LLModelDescriptorTracked.Empty;
		/// <summary>
		/// The model to use for chat.
		/// </summary>
		public LLModelDescriptorTracked ChatModel
		{
			get => _chatModel;
			set => SetProperty(ref _chatModel, value);
		}

		private LLModelDescriptorTracked _agenticToolsModel = LLModelDescriptorTracked.Empty;
		/// <summary>
		/// The model to use for <see cref="AgenticToolModule"/>.
		/// </summary>
		public LLModelDescriptorTracked AgenticToolsModel
		{
			get => _agenticToolsModel;
			set => SetProperty(ref _agenticToolsModel, value);
		}

		private LLModelDescriptorTracked _routerModel = LLModelDescriptorTracked.Empty;
		/// <summary>
		/// The model to use for agentic routing in the <see cref="AdaptiveAgentExecutionStage"/>.
		/// </summary>
		public LLModelDescriptorTracked AgenticRouterModel
		{
			get => _routerModel;
			set => SetProperty(ref _routerModel, value);
		}

		private LLModelDescriptorTracked _visionModel = LLModelDescriptorTracked.Empty;
		/// <summary>
		/// The model to use for vision and image-understanding tasks.
		/// </summary>
		public LLModelDescriptorTracked VisionModel
		{
			get => _visionModel;
			set => SetProperty(ref _visionModel, value);
		}
	}
}