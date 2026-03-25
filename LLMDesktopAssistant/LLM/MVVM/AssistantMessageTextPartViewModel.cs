using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.MVVM;
using System.ComponentModel;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(AssistantMessageTextPartView))]
	public class AssistantMessageTextPartViewModel : AssistantMessagePartViewModel
	{
		private string _text = string.Empty;
		public string Text
		{
			get => _text;
			set => SetProperty(ref _text, value);
		}

		private bool _completed = true;
		public bool Completed
		{
			get => _completed;
			set => SetProperty(ref _completed, value);
		}

		public AssistantMessageTextPartViewModel()
		{
		}

		public AssistantMessageTextPartViewModel(AssistantMessage message)
		{
			Text = message.Content ?? string.Empty;

			if (!message.IsCompleted)
			{
				Completed = false;

				void PropertyChangedHandler(object? s, PropertyChangedEventArgs e)
				{
					InvokeUI(() =>
					{
						Text = message.Content ?? string.Empty;
					});
				}

				message.PropertyChanged += PropertyChangedHandler;
				message.CompletionToken.OnCompleted(() =>
				{
					Completed = true;
					message.PropertyChanged -= PropertyChangedHandler;
				});
			}
			else
			{
				Completed = true;
			}
		}
	}
}