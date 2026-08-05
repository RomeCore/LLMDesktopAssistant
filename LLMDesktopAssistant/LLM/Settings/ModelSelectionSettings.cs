using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.Tools.Implementations;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Represents the model selection group of a chat: the models used for the main chat,
	/// agentic routing, agentic tools and vision tasks.
	/// </summary>
	public class ModelSelectionSettings : NotifyPropertyChanged
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
