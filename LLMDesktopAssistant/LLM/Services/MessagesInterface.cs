using LLMDesktopAssistant.LLM.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Interface for smart retrieval of chat messages.
	/// </summary>
	/// <param name="chat"></param>
	[ChatService]
	public class MessagesInterface(Chat chat)
	{
		/// <summary>
		/// Groups messages into rounds.
		/// A round = [one or more consecutive user messages] + [one or more consecutive assistant messages].
		/// </summary>
		/// <param name="messages">List of messages to group.</param>
		public static List<List<BranchedMessage>> GroupMessagesIntoRounds(IReadOnlyList<BranchedMessage> messages)
		{
			var rounds = new List<List<BranchedMessage>>();
			if (messages.Count == 0)
				return rounds;

			List<BranchedMessage>? currentRound = null;
			bool? lastWasUser = null;

			foreach (var branched in messages)
			{
				bool isUser = branched.Message is Domain.UserMessage;
				bool isAssistant = branched.Message is Domain.AssistantMessage;

				if (isUser)
				{
					// Start a new round if previous was assistant, or first message is user
					if (lastWasUser == false || lastWasUser == null)
					{
						currentRound = [branched];
						rounds.Add(currentRound);
					}
					else
					{
						currentRound?.Add(branched);
					}
					lastWasUser = true;
				}
				else if (isAssistant)
				{
					if (lastWasUser == true || lastWasUser == null)
					{
						currentRound = [branched];
						rounds.Add(currentRound);
					}
					else
					{
						currentRound?.Add(branched);
					}
					lastWasUser = false;
				}
			}

			return rounds;
		}

		/// <summary>
		/// Groups messages into rounds.
		/// A round = [one or more consecutive user messages] + [one or more consecutive assistant messages].
		/// </summary>
		/// <param name="messages">List of messages to group.</param>
		/// <param name="maxLastRounds">Maximum number of rounds from end to return. If zero, all rounds are returned.</param>
		public static List<List<BranchedMessage>> GroupMessagesIntoRounds(IReadOnlyList<BranchedMessage> messages, int maxLastRounds)
		{
			var rounds = GroupMessagesIntoRounds(messages);

			if (maxLastRounds > 0 && maxLastRounds < rounds.Count)
				return rounds.Skip(rounds.Count - maxLastRounds).ToList();

			return rounds;
		}

		public List<List<BranchedMessage>> GroupMessagesIntoRounds()
		{
			return GroupMessagesIntoRounds(chat.Messages);
		}

		public List<List<BranchedMessage>> GroupMessagesIntoRounds(int maxLastRounds)
		{
			return GroupMessagesIntoRounds(chat.Messages, maxLastRounds);
		}
	}
}