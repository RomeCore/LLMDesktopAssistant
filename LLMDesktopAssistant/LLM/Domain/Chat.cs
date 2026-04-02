using System.Collections.ObjectModel;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.ToolModules;
using LLMDesktopAssistant.Utils;
using Microsoft.VisualBasic;

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