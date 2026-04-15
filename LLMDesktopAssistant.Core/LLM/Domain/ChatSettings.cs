using LLMDesktopAssistant.Core.Settings;
using LLMDesktopAssistant.Core.Utils;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients;
using System.IO;

namespace LLMDesktopAssistant.Core.LLM.Domain
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

		private LLModelDescriptorTracked _agenticModel = LLModelDescriptorTracked.Empty;
		/// <summary>
		/// The model to to use for 
		/// </summary>
		public LLModelDescriptorTracked AgenticModel
		{
			get => _agenticModel;
			set => SetProperty(ref _agenticModel, value);
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
		/// Returns the working directory for the chatbot. If no working directory is specified, returns the default directory.
		/// </summary>
		public string GetWorkingDirectory() => WorkingDirectory ?? Path.GetFullPath(Directories.DefaultWorkingDirectory);

		private string? _pythonVenvActivateScriptPath;
		/// <summary>
		/// The path to the script that activates a python virtual environment.
		/// </summary>
		public string? PythonVenvActivateScriptPath
		{
			get => _pythonVenvActivateScriptPath;
			set => SetProperty(ref _pythonVenvActivateScriptPath, value);
		}

		private bool _enableTools = true;
		/// <summary>
		/// Whether to use tools in the chat.
		/// </summary>
		public bool EnableTools
		{
			get => _enableTools;
			set => SetProperty(ref _enableTools, value);
		}

		private readonly RangeObservableCollection<ToolChange> _toolChanges = [];
		/// <summary>
		/// Gets or sets the tool changes.
		/// </summary>
		public ICollection<ToolChange> ToolChanges
		{
			get => _toolChanges;
			set => _toolChanges.Reset(value);
		}

		private bool _enableMcp = true;
		/// <summary>
		/// Whether to use the MCP servers for additional tools and resources.
		/// </summary>
		public bool EnableMcp
		{
			get => _enableMcp;
			set => SetProperty(ref _enableMcp, value);
		}

		private readonly RangeObservableCollection<Guid> _usedMcpServers = [];
		/// <summary>
		/// Gets or sets the used MCP server Ids.
		/// </summary>
		public ICollection<Guid> UsedMcpServers
		{
			get => _usedMcpServers;
			set => _usedMcpServers.Reset(value);
		}
	}
}