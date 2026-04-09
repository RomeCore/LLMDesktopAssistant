using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.MVVM;
using System.ComponentModel;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace LLMDesktopAssistant.LLM.MVVM.Messages
{
	[ViewModelFor(typeof(AssistantMessageReasoningPartView))]
	public class AssistantMessageReasoningPartViewModel : AssistantMessagePartViewModel
	{
		private DispatcherOperation? _currentUpdateOperation;

		private string _reasoningText = string.Empty;
		public string ReasoningText
		{
			get => _reasoningText;
			set => SetProperty(ref _reasoningText, value);
		}

		private bool _completed = true;
		public bool Completed
		{
			get => _completed;
			set => SetProperty(ref _completed, value);
		}
		public bool NotCompleted => !_completed;

		public AssistantMessageReasoningPartViewModel()
		{
		}

		public AssistantMessageReasoningPartViewModel(AssistantMessage message)
		{
			ReasoningText = message.ReasoningContent ?? string.Empty;

			if (!message.IsCompleted)
			{
				Completed = false;

				void PropertyChangedHandler(object? s, PropertyChangedEventArgs e)
				{
					_currentUpdateOperation = BeginInvokeUI(() =>
					{
						_currentUpdateOperation?.Abort();
						ReasoningText = message.ReasoningContent ?? string.Empty;
					});
				}

				message.PropertyChanged += PropertyChangedHandler;
				message.CompletionToken.OnCompleted(() =>
				{
					BeginInvokeUI(() =>
					{
						_currentUpdateOperation?.Abort();
						_currentUpdateOperation = null;
						Completed = true;
						RaisePropertyChanged(nameof(NotCompleted));
					});

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