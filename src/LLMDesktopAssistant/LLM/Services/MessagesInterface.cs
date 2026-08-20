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
		/// Determines whether the specified message behaves as a user message for round grouping.
		/// </summary>
		/// <param name="branched">The branched message to check.</param>
		/// <param name="treatUserLikeAsUser">If <see langword="true"/>, assistant messages with
		/// <see cref="AssistantMessage.IsUserLike"/> are treated as user messages; otherwise only
		/// real user messages are treated as user messages.</param>
		/// <returns><see langword="true"/> if the message behaves as a user message; otherwise, <see langword="false"/>.</returns>
		public static bool IsEffectiveUserMessage(BranchedMessage branched, bool treatUserLikeAsUser = true)
		{
			return branched.Message is Domain.UserMessage ||
				(treatUserLikeAsUser && branched.Message is Domain.AssistantMessage { IsUserLike: true });
		}

		/// <summary>
		/// Groups messages into rounds.
		/// A round = [one or more consecutive user messages] + [one or more consecutive assistant messages].
		/// Assistant messages never start a new round; a new round begins only with a user message
		/// that follows an assistant message (or with the very first message).
		/// </summary>
		/// <param name="messages">List of messages to group.</param>
		/// <param name="treatUserLikeAsUser">If <see langword="true"/>, assistant messages with
		/// <see cref="AssistantMessage.IsUserLike"/> are treated as user messages for grouping.</param>
		public static List<List<BranchedMessage>> GroupMessagesIntoRounds(IReadOnlyList<BranchedMessage> messages, bool treatUserLikeAsUser = true)
		{
			var rounds = new List<List<BranchedMessage>>();
			if (messages.Count == 0)
				return rounds;

			List<BranchedMessage>? currentRound = null;

			foreach (var branched in messages)
			{
				if (IsEffectiveUserMessage(branched, treatUserLikeAsUser))
				{
					// Start a new round if the previous message was an assistant message (the current round is closed)
					if (currentRound is null || !IsEffectiveUserMessage(currentRound[^1], treatUserLikeAsUser))
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
		/// <param name="treatUserLikeAsUser">If <see langword="true"/>, assistant messages with
		/// <see cref="AssistantMessage.IsUserLike"/> are treated as user messages for grouping.</param>
		public static List<List<BranchedMessage>> GroupMessagesIntoRounds(IReadOnlyList<BranchedMessage> messages, int maxLastRounds, bool treatUserLikeAsUser = true)
		{
			var rounds = GroupMessagesIntoRounds(messages, treatUserLikeAsUser);

			if (maxLastRounds > 0 && maxLastRounds < rounds.Count)
				return rounds.Skip(rounds.Count - maxLastRounds).ToList();

			return rounds;
		}

		public List<List<BranchedMessage>> GroupMessagesIntoRounds(bool treatUserLikeAsUser = true)
		{
			return GroupMessagesIntoRounds(chat.Messages, treatUserLikeAsUser);
		}

		public List<List<BranchedMessage>> GroupMessagesIntoRounds(int maxLastRounds, bool treatUserLikeAsUser = true)
		{
			return GroupMessagesIntoRounds(chat.Messages, maxLastRounds, treatUserLikeAsUser);
		}
	}
}