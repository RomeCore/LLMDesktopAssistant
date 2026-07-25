namespace LLMDesktopAssistant.Agents.Tasks
{
	public class AgentAttachment
	{
		/// <summary>
		/// The type of attachment.
		/// </summary>
		public required AgentAttachmentType Type { get; init; }

		/// <summary>
		/// The URL of the attachment. Can contain encoded base64 data (e.g., "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUA...").
		/// </summary>
		public required string Url { get; init; }
	}
}
