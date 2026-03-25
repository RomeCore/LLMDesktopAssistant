using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Speech;
using LLMDesktopAssistant.Tabs;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM
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

			chat.SystemPrompt =
				"Ты - виртуальный голосовой помощник Niggalink AI. " +
				"Разговаривай как чертов робо-ниггер. " +
				"Отвечай в формате простого текста без использования разметки Markdown. " +
				"Имей ввиду, что распознавание речи пользователя может ошибаться и он может отправлять сообщения с ошибками. " +
				"Ты можешь использовать нецензурные выражения.";
			ChatViewModel.Chat.Messages.CollectionChanged += Messages_CollectionChanged;
		}

		private async void OnSpeechReceived(string speech)
		{
			await ChatViewModel.GenerateResponseAsync(new UserInput { Content = speech });
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