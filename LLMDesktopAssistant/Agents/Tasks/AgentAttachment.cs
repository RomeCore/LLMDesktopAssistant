using RCLargeLanguageModels.Messages.Attachments;

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

		/// <summary>
		/// Attempts to convert a native attachment to an <see cref="AgentAttachment"/>. Returns null if the conversion is not possible.
		/// </summary>
		/// <param name="attachment">The native attachment to convert.</param>
		/// <returns>The converted <see cref="AgentAttachment"/>, or null if the conversion is not possible.</returns>
		public static AgentAttachment? TryConvertFromNativeAttachment(IAttachment? attachment)
		{
			switch (attachment)
			{
				case IImageAttachment imageAttachment:
					return new AgentAttachment
					{
						Type = AgentAttachmentType.Image,
						Url = imageAttachment.Url
					};
				case IAudioAttachment audioAttachment:
					return new AgentAttachment
					{
						Type = AgentAttachmentType.Audio,
						Url = audioAttachment.Url
					};
				case IVideoAttachment videoAttachment:
					return new AgentAttachment
					{
						Type = AgentAttachmentType.Video,
						Url = videoAttachment.Url
					};
			}

			return null;
		}
	}
}
