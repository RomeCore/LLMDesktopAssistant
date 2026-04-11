using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LLMDesktopAssistant.Core.LLM.Data;
using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.LLM.Services;
using LLMDesktopAssistant.Core.Modules;
using LLMDesktopAssistant.Core.MVVM;
using LLMDesktopAssistant.Core.Speech;
using LLMDesktopAssistant.Core.Tabs;
using LLMDesktopAssistant.Core.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace LLMDesktopAssistant.Core.LLM.MVVM
{
	// [TabTool("voice_chat", Order = -10)]
	[ViewModelFor(typeof(VoiceChatView))]
	public class VoiceChatViewModel : ViewModelBase
	{
		public ChatViewModel ChatViewModel { get; }

		public VoiceChatViewModel(Chat chat)
		{
			ChatViewModel = new ChatViewModel(chat);

			var tracker = ModuleManager.GetDynamicTracker<IUserSpeechProvider>();
			tracker.OnChanged += (o, n) =>
			{
				if (o != null)
					o.OnSpeechReceived -= OnSpeechReceived;
				if (n != null)
					n.OnSpeechReceived += OnSpeechReceived;
			};
			if (tracker.Module != null)
				tracker.Module.OnSpeechReceived += OnSpeechReceived;

			ChatViewModel.Chat.Messages.CollectionChanged += Messages_CollectionChanged;
		}

		private async void OnSpeechReceived(string speech)
		{
			await ChatViewModel.Chat.Services.GetRequiredService<IChatOperationService>().SendUserInputAsync(new UserInput { Content = speech });
		}

		private void Messages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (var message in e.NewItems.OfType<AssistantMessage>())
				{
					SpeakMessage(message);
				}
			}
		}

		private async void SpeakMessage(AssistantMessage message)
		{
			var buffer = new StringBuilder();

			/*await foreach (var part in message)
			{
				if (string.IsNullOrEmpty(part.DeltaContent))
					continue;

				buffer.Append(part.DeltaContent);
				var speechableText = MarkdownParser.ParseSpeechablePlainTextStreaming(buffer);
				if (!string.IsNullOrWhiteSpace(speechableText))
					SpeechQueue.Enqueue(speechableText);
			}*/

			var remaining = buffer.ToString();
			var remainingSpeechableText = MarkdownParser.ParseSpeechablePlainText(remaining);
			if (!string.IsNullOrWhiteSpace(remainingSpeechableText))
				SpeechQueue.Enqueue(remainingSpeechableText);
		}
	}
}