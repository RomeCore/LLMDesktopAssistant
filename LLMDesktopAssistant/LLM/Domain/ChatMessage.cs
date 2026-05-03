using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents a base class for chat messages.
	/// </summary>
	public abstract class ChatMessage : NotifyPropertyChanged
	{
		public DateTime CreatedAt { get; init; } = DateTime.Now;

		private string _content = string.Empty;
		/// <summary>
		/// Gets or sets the content of the message.
		/// </summary>
		public string Content
		{
			get => _content;
			set => SetProperty(ref _content, value);
		}

		private string? _summaryOfPrevMessages;
		/// <summary>
		/// Gets or sets the summary of previous messages. Used for memory context.
		/// </summary>
		public string? SummaryOfPrevMessages
		{
			get => _summaryOfPrevMessages;
			set => SetProperty(ref _summaryOfPrevMessages, value);
		}

		private bool _hasContextShield;
		/// <summary>
		/// Gets or sets a flag indicating whether the message has a context shield.
		/// Context shield means that this message and all previous will not be included in the generation.
		/// </summary>
		public bool HasContextShield
		{
			get => _hasContextShield;
			set => SetProperty(ref _hasContextShield, value);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				foreach (var viewModel in AdditionalViewModels)
					viewModel.Dispose();
		}

		/// <summary>
		/// The collection of additional view models associated with this chat message.
		/// These can be used for displaying extra information in the UI or store additional data.
		/// </summary>
		public RangeObservableCollection<AdditionalMessageViewModel> AdditionalViewModels { get; } = new() { RaiseInUIThread = true };
	}
}