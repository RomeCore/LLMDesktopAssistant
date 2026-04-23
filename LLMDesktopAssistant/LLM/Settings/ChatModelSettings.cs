using LLMDesktopAssistant.Settings;
using RCLargeLanguageModels.Clients;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Settings related to language models used in chat.
	/// </summary>
	public class ChatModelSettings : NotifyPropertyChanged
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

		private LLModelDescriptorTracked _agenticModel = LLModelDescriptorTracked.Empty;
		/// <summary>
		/// The model to use for agentic tasks.
		/// </summary>
		public LLModelDescriptorTracked AgenticModel
		{
			get => _agenticModel;
			set => SetProperty(ref _agenticModel, value);
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