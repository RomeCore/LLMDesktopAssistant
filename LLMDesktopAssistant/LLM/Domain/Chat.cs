using System.Collections.ObjectModel;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.ToolModules;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels;

namespace LLMDesktopAssistant.LLM.Domain
{
	public class ChatSettings : SettingsObject
	{
		private LLModelDescriptor? _chatModel;
		public LLModelDescriptor? ChatModel
		{
			get => _chatModel;
			set => SetProperty(ref _chatModel, value);
		}

		private LLModelDescriptor? _summarizerModel;
		public LLModelDescriptor? SummarizerModel
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

	/// <summary>
	/// Represents a chat session.
	/// </summary>
	public class Chat(IServiceProvider services) : NotifyPropertyChanged
	{
		/// <summary>
		/// Gets the service provider used to resolve dependencies.
		/// </summary>
		public IServiceProvider Services { get; } = services;

		/// <summary>
		/// Gets or sets the unique identifier for the chat session. Used mostly for database purposes.
		/// </summary>
		public int ChatId { get; set; }

		/// <summary>
		/// The collection of messages in the chat session.
		/// </summary>
		public RangeObservableCollection<BranchedMessage> Messages { get; } = [];

		private CancellationTokenSource? _generationCts;
		/// <summary>
		/// Gets or sets the current message generation <see cref="CancellationTokenSource"/>.
		/// Use this  to cancel the current message generation (inference) task.
		/// </summary>
		public CancellationTokenSource? GenerationCts
		{
			get => _generationCts;
			set => SetProperty(ref _generationCts, value);
		}

		/// <summary>
		/// Gets or sets the list of tool modules that are available for use in the chat session.
		/// </summary>
		public List<ToolModule> AdditionalToolModules { get; set; } = [];


		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				foreach (var message in Messages)
				{
					message.Dispose();
				}
			}
		}
	}
}