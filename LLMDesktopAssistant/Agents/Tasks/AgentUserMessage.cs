using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents.Tasks
{
	public class AgentUserMessage : AgentChatMessage
	{
		private RangeObservableCollection<AgentAttachment> _attachments = [];
		/// <summary>
		/// A collection of attachments associated with the user's message.
		/// </summary>
		public RangeObservableCollection<AgentAttachment> Attachments
		{
			get => _attachments;
			set => _attachments.Reset(value);
		}
	}
}
