using Avalonia.Threading;
using LLMDesktopAssistant.LLM.Domain;
using System.ComponentModel;

namespace LLMDesktopAssistant.LLM.Messages
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

		public AssistantMessageReasoningPartViewModel()
		{
		}

		public AssistantMessageReasoningPartViewModel(AssistantMessage message)
		{
			ReasoningText = message.ReasoningContent ?? string.Empty;

			Completed = message.IsCompleted;
			if (message.IsCompleted) return;

			void PropertyChangedHandler(object? s, PropertyChangedEventArgs e)
			{
				Completed = e.PropertyName != nameof(message.ReasoningContent);
				if (Completed)
					return;

				_currentUpdateOperation?.Abort();
				_currentUpdateOperation = InvokeUIAsync(() =>
				{
					ReasoningText = message.ReasoningContent ?? string.Empty;
				});
			}

			message.PropertyChanged += PropertyChangedHandler;
			message.CompletionToken.OnCompleted(() =>
			{
				InvokeUIAsync(() =>
				{
					_currentUpdateOperation?.Abort();
					_currentUpdateOperation = null;
					Completed = true;
				});

				message.PropertyChanged -= PropertyChangedHandler;
			});
		}
	}
}
