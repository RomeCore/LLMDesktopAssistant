using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Messages;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.ToolModules;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(ChatView))]
	public class ChatViewModel : ViewModelBase
	{
		/// <summary>
		/// Gets the message sequence that represents the conversation history.
		/// </summary>
		public MessageSequenceViewModel MessageSequence { get; }

		/// <summary>
		/// Gets the conversation manager that manages the current conversation.
		/// </summary>
		public Chat Chat { get; }

		/// <summary>
		/// Gets the user input to be sent in the next conversation turn.
		/// </summary>
		public UserInputViewModel UserInput { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatViewModel"/> class.
		/// </summary>
		public ChatViewModel(Chat chat)
		{
			Chat = chat;
			UserInput = new UserInputViewModel(this);
			MessageSequence = new MessageSequenceViewModel(this);
		}
	}
}