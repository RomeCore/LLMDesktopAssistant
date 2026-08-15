using LLMDesktopAssistant.LLM.Domain;

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
		/// Assistant messages never start a new round; a new round begins only with a user message
		/// that follows an assistant message (or with the very first message).
		/// </summary>
		/// <param name="messages">List of messages to group.</param>
		public static List<List<BranchedMessage>> GroupMessagesIntoRounds(IReadOnlyList<BranchedMessage> messages)
		{
			var rounds = new List<List<BranchedMessage>>();
			if (messages.Count == 0)
				return rounds;

			List<BranchedMessage>? currentRound = null;

			foreach (var branched in messages)
			{
				if (branched.Message is Domain.UserMessage)
				{
					// Start a new round if the previous message was an assistant message (the current round is closed)
					if (currentRound is null || currentRound[^1].Message is not Domain.UserMessage)
					{
						currentRound = [branched];
						rounds.Add(currentRound);
					}
					else
					{
						currentRound.Add(branched);
					}
				}
				else
				{
					// Assistant messages complete the current round and never start a new one
					if (currentRound is null)
					{
						currentRound = [branched];
						rounds.Add(currentRound);
					}
					else
					{
						currentRound.Add(branched);
					}
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