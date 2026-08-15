using LLMDesktopAssistant.Agents.Tasks;
using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.LLM.MVVM.ContextTabs;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;

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

		private ChatDatabase _database = null!;
		/// <summary>
		/// Gets the database used to store this chat session's data.
		/// </summary>
		public ChatDatabase ChatDatabase
		{
			get => _database;
			set
			{
				if (_database != null)
					throw new InvalidOperationException("ChatDatabase cannot be changed once set.");
				_database = value;
			}
		}

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

		private string _topic = string.Empty;
		/// <summary>
		/// Gets or sets the topic/category of the chat session.
		/// This is a human-readable category like "coding", "roleplay", "dnd", etc.
		/// The color for this topic is generated from its hash for consistent UI display.
		/// </summary>
		public string Topic
		{
			get => _topic;
			set => SetProperty(ref _topic, value);
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
		/// Gets the collection of agent tasks associated with this chat session.
		/// </summary>
		public RangeObservableCollection<AgentTask> AgentTasks { get; } = [];

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