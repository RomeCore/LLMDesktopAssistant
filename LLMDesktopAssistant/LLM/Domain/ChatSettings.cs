using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients;

namespace LLMDesktopAssistant.LLM.Domain
{
	public class ChatSettings : SettingsObject
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

		private LLModelDescriptorTracked _summarizerModel = LLModelDescriptorTracked.Empty;
		/// <summary>
		/// The model to use for summarizing the conversation for compacting.
		/// </summary>
		public LLModelDescriptorTracked SummarizerModel
		{
			get => _summarizerModel;
			set => SetProperty(ref _summarizerModel, value);
		}

		private string? _systemInstructions;
		/// <summary>
		/// Instructions to the model on how it should behave.
		/// </summary>
		public string? SystemInstructions
		{
			get => _systemInstructions;
			set => SetProperty(ref _systemInstructions, value);
		}

		private string? _personality;
		/// <summary>
		/// The personality of the chatbot. This can be used to influence the behavior and tone of the chatbot.
		/// </summary>
		public string? Personality
		{
			get => _personality;
			set => SetProperty(ref _personality, value);
		}

		private string? _workingDirectory;
		/// <summary>
		/// The working directory for the chatbot. This can be used to store files and execute commands, python scripts etc.
		/// </summary>
		public string? WorkingDirectory
		{
			get => _workingDirectory;
			set => SetProperty(ref _workingDirectory, value);
		}

		/// <summary>
		/// Returns the working directory for the chatbot. If no working directory is specified, returns the current directory.
		/// </summary>
		public string GetWorkingDirectory() => WorkingDirectory ?? Environment.CurrentDirectory;

		private readonly RangeObservableCollection<ToolChange> _toolChanges = [];
		/// <summary>
		/// Gets or sets the tool changes.
		/// </summary>
		public ICollection<ToolChange> ToolChanges
		{
			get => _toolChanges;
			set => _toolChanges.Reset(value);
		}

		private readonly RangeObservableCollection<Guid> _usedMcpServers = [];
		/// <summary>
		/// Gets or sets the used mcp server Ids.
		/// </summary>
		public ICollection<Guid> UsedMcpServers
		{
			get => _usedMcpServers;
			set => _usedMcpServers.Reset(value);
		}
	}
}