using Avalonia.Collections;
using LLMDesktopAssistant.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		/// <summary>
		/// The collection of additional view models associated with this chat message.
		/// These can be used for displaying extra information in the UI or store additional data.
		/// </summary>
		public RangeObservableCollection<AdditionalMessageViewModel> AdditionalViewModels { get; } = new() { RaiseInUIThread = true };
	}
}