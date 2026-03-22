using System;
using System.Collections.Generic;
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
	}
}