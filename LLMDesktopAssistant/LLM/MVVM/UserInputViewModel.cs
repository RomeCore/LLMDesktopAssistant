using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(UserInputView))]
	public class UserInputViewModel : ViewModelBase
	{
		private string _text = string.Empty;
		/// <summary>
		/// Gets or sets the user input to be sent in the next conversation turn.
		/// </summary>
		public string Text
		{
			get => _text;
			set => SetProperty(ref _text, value);
		}

		/// <summary>
		/// Command to send a message. This command is bound to the UI and triggers the SendMessage method.
		/// </summary>
		public ICommand SendMessageCommand { get; }

		public UserInputViewModel(ICommand sendMessageCommand)
		{
			SendMessageCommand = sendMessageCommand;
		}



		public UserInput Peek()
		{
			return new UserInput
			{
				Content = _text,
			};
		}

		public UserInput? Pop()
		{
			return null;
		}

		public void Push(UserInput userInput)
		{

		}
	}
}