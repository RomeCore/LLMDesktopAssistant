using DocumentFormat.OpenXml.Office2010.ExcelAc;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents a base class for chat messages.
	/// </summary>
	public abstract class ChatMessage : NotifyPropertyChanged
	{
		public required DateTime CreatedAt { get; init; }

		private string _content = string.Empty;
		/// <summary>
		/// Gets or sets the content of the message.
		/// </summary>
		public string Content
		{
			get => _content;
			set => SetProperty(ref _content, value);
		}

		private RangeObservableCollection<Attachment> _attachments = [];
		/// <summary>
		/// Gets or sets the attachments associated with the message.
		/// </summary>
		public RangeObservableCollection<Attachment> Attachments
		{
			get => _attachments;
			set => _attachments.Reset(value);
		}

		private AdditionalMessageViewModelCollection _additionalViewModels = [];
		/// <summary>
		/// The collection of additional view models associated with this chat message.
		/// These can be used for displaying extra information in the UI or store additional data.
		/// </summary>
		public AdditionalMessageViewModelCollection AdditionalViewModels
		{
			get => _additionalViewModels;
			set => _additionalViewModels.Reset(value);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				foreach (var viewModel in AdditionalViewModels)
					viewModel.Dispose();
		}
	}
}