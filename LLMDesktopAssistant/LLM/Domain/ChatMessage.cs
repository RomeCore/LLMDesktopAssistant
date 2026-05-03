using DocumentFormat.OpenXml.Office2010.ExcelAc;

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
		public AdditionalMessageViewModelCollection AdditionalViewModels { get; } = [];
	}
}