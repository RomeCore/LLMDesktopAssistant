using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Controls.Dialogs
{
	public class DialogViewModel : ViewModelBase
	{
		private object? _content;
		public  object? Content
		{
			get => _content;
			set => SetProperty(ref _content, value);
		}

		private string? _title;
		public string? Title
		{
			get => _title;
			set => SetProperty(ref _title, value);
		}
	}
}