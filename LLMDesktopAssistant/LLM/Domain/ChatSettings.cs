using LLMDesktopAssistant.Settings;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients;

namespace LLMDesktopAssistant.LLM.Domain
{
	public class ChatSettings : SettingsObject
	{
		private LLModelDescriptorTracked _chatModel = LLModelDescriptorTracked.Empty;
		public LLModelDescriptorTracked ChatModel
		{
			get => _chatModel;
			set => SetProperty(ref _chatModel, value);
		}

		private LLModelDescriptorTracked _summarizerModel = LLModelDescriptorTracked.Empty;
		public LLModelDescriptorTracked SummarizerModel
		{
			get => _summarizerModel;
			set => SetProperty(ref _summarizerModel, value);
		}

		private string? _systemInstructions;
		public string? SystemInstructions
		{
			get => _systemInstructions;
			set => SetProperty(ref _systemInstructions, value);
		}
	}
}