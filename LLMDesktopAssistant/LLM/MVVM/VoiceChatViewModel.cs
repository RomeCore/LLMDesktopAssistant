using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LLMDesktopAssistant.LLM.Conversations;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Speech;
using LLMDesktopAssistant.Tabs;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM.MVVM
{
	// [TabTool("voice_chat", Order = -10)]
	[ViewModelFor(typeof(VoiceChatView))]
	public class VoiceChatViewModel : ViewModelBase
	{
		public ChatViewModel ChatViewModel { get; }

		public VoiceChatViewModel(ConversationManager conversationManager)
		{
			ChatViewModel = new ChatViewModel(conversationManager);

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

			ChatViewModel.SystemPrompt =
				"Ты - виртуальный голосовой помощник Niggalink AI. " +
				"Разговаривай как чертов робо-ниггер. " +
				"Отвечай в формате простого текста без использования разметки Markdown. " +
				"Имей ввиду, что распознавание речи пользователя может ошибаться и он может отправлять сообщения с ошибками. " +
				"Ты можешь использовать нецензурные выражения.";
			ChatViewModel.ConversationManager.CollectionChanged += Messages_CollectionChanged;
		}

		private async void OnSpeechReceived(string obj)
		{
			await ChatViewModel.SendMessageAsync(new UserMessage(obj));
		}

		private void Messages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (var message in e.NewItems.OfType<IAssistantMessage>())
				{
					if (message is PartialAssistantMessage partialMessage)
						SpeakMessage(partialMessage);
					else
						SpeakMessage(message);
				}
			}
		}

		private async void SpeakMessage(PartialAssistantMessage message)
		{
			var buffer = new StringBuilder();

			await foreach (var part in message)
			{
				if (string.IsNullOrEmpty(part.DeltaContent))
					continue;

				buffer.Append(part.DeltaContent);
				var speechableText = MarkdownParser.ParseSpeechablePlainTextStreaming(buffer);
				if (!string.IsNullOrWhiteSpace(speechableText))
					SpeechQueue.Enqueue(speechableText);
			}

			var remaining = buffer.ToString();
			var remainingSpeechableText = MarkdownParser.ParseSpeechablePlainText(remaining);
			if (!string.IsNullOrWhiteSpace(remainingSpeechableText))
				SpeechQueue.Enqueue(remainingSpeechableText);
		}

		private void SpeakMessage(IAssistantMessage message)
		{
			var speechableText = MarkdownParser.ParseSpeechablePlainText(message.Content ?? string.Empty);
			if (!string.IsNullOrWhiteSpace(speechableText))
				SpeechQueue.Enqueue(speechableText);
		}
	}
}