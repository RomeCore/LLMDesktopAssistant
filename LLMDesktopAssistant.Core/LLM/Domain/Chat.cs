using System.Collections.ObjectModel;
using LLMDesktopAssistant.Core.Settings;
using LLMDesktopAssistant.Core.ToolModules;
using LLMDesktopAssistant.Core.Utils;
using LLMDesktopAssistant.Core.LLM.Services;

namespace LLMDesktopAssistant.Core.LLM.Domain
{
	/// <summary>
	/// Represents a chat session.
	/// </summary>
	public class Chat(IServiceProvider services) : NotifyPropertyChanged
	{
		/// <summary>
		/// Gets the service provider used to resolve dependencies.
		/// </summary>
		public IServiceProvider Services { get; } = services;

		private int _chatId = -1;
		/// <summary>
		/// Gets or sets the unique identifier for the chat session. Used mostly for database purposes.
		/// </summary>
		public int ChatId
		{
			get => _chatId;
			set
			{
				if (_chatId != -1)
					throw new InvalidOperationException("ChatId cannot be changed once set.");
				_chatId = value;
			}
		}

		private bool _isTemporary = false;
		/// <summary>
		/// Gets or sets a value indicating whether the chat session is temporary.
		/// Temporary chats will be removed when user opens application again.
		/// </summary>
		public bool IsTemporary
		{
			get => _isTemporary;
			set => SetProperty(ref _isTemporary, value);
		}

		/// <summary>
		/// The collection of messages in the chat session.
		/// These are managed by <see cref="IChatStorageService"/>.
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

		private ChatSettings _settings = SettingsManager.Get<ChatSettings>();
		/// <summary>
		/// Gets or sets the chat settings.
		/// </summary>
		public ChatSettings Settings
		{
			get => _settings;
			set => SetProperty(ref _settings, value);
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