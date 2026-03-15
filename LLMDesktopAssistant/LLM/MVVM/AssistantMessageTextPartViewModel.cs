using LLMDesktopAssistant.MVVM;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tasks;

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

		public AssistantMessageTextPartViewModel(IAssistantMessage message)
		{
			Text = message.Content ?? string.Empty;

			if (message is PartialAssistantMessage pMessage)
			{
				if (!pMessage.CompletionToken.IsCompleted)
				{
					Completed = false;
					void OnPartAdded(object? s, AssistantMessageDelta e)
					{
						App.Current.Dispatcher.Invoke(() =>
						{
							Text = message.Content ?? string.Empty;
						});
					}
					void OnCompleted(object? s, CompletedEventArgs e)
					{
						pMessage.PartAdded -= OnPartAdded;
						pMessage.Completed -= OnCompleted;
						Completed = true;
						Console.WriteLine(Text);
					}
					pMessage.PartAdded += OnPartAdded;
					pMessage.Completed += OnCompleted;
				}
				else
				{
					Completed = true;
					Console.WriteLine(Text);
				}
			}
		}
	}
}