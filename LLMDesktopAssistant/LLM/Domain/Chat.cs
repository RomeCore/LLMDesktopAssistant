using System.Collections.ObjectModel;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Settings;
using Material.Icons;
using LLMDesktopAssistant.LLM.MVVM.ContextTabs;

namespace LLMDesktopAssistant.LLM.Domain
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

		private string _title = string.Empty;
		/// <summary>
		/// Gets or sets the title of the chat session.
		/// </summary>
		public string Title
		{
			get => _title;
			set => SetProperty(ref _title, value);
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
		/// Use this to cancel the current message generation (inference) task.
		/// </summary>
		public CancellationTokenSource? GenerationCts
		{
			get => _generationCts;
			set => SetProperty(ref _generationCts, value);
		}

		private MaterialIconKind _statusIcon;
		/// <summary>
		/// Gets or sets the icon kind to display in the status bar of the chat window.
		/// </summary>
		public MaterialIconKind StatusIcon
		{
			get => _statusIcon;
			set => SetProperty(ref _statusIcon, value);
		}

		private string? _statusText;
		/// <summary>
		/// Gets or sets the text to display in the status bar of the chat window.
		/// </summary>
		public string? StatusText
		{
			get => _statusText;
			set => SetProperty(ref _statusText, value);
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

		private ChatContextTabViewModelCollection _contextTabs = [];
		/// <summary>
		/// Gets or sets the collection of context tabs associated with this chat session.
		/// </summary>
		public ChatContextTabViewModelCollection ContextTabs
		{
			get => _contextTabs;
			set => _contextTabs.Reset(value);
		}

		/// <summary>
		/// Gets or sets the list of tool modules that are available for use in the chat session.
		/// </summary>
		public List<ToolModule> AdditionalTools { get; set; } = [];



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