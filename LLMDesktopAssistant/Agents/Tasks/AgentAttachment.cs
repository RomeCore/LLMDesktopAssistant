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
		/// The source of the attachment, if applicable. This can be any object that provides additional context or metadata about the attachment.
		/// </summary>
		public object? Source { get; init; }

		/// <summary>
		/// Creates a new instance of <see cref="AgentAttachment"/> from base64 data.
		/// </summary>
		/// <param name="type">The type of attachment.</param>
		/// <param name="format">The data format (e.g., "png").</param>
		/// <param name="base64Data">The base64 encoded data.</param>
		/// <param name="source">The source of the attachment, if applicable.</param>
		/// <returns>A new instance of <see cref="AgentAttachment"/>.</returns>
		public static AgentAttachment FromBase64(AgentAttachmentType type, string format, string base64Data, object? source = null)
		{
			var dataType = type switch
			{
				AgentAttachmentType.Image => "image",
				AgentAttachmentType.Audio => "audio",
				AgentAttachmentType.Video => "video",
				_ => throw new ArgumentOutOfRangeException(nameof(type), "Invalid attachment type.")
			};
			return new AgentAttachment
			{
				Type = type,
				Url = $"data:{dataType}/{format};base64,{base64Data}",
				Source = source
			};
		}

		/// <summary>
		/// Attempts to convert a native attachment to an <see cref="AgentAttachment"/>. Returns null if the conversion is not possible.
		/// </summary>
		/// <param name="attachment">The native attachment to convert.</param>
		/// <returns>The converted <see cref="AgentAttachment"/>, or null if the conversion is not possible.</returns>
		public static AgentAttachment? TryConvertFromNativeAttachment(IAttachment? attachment)
		{
			return TryConvertFromNativeAttachment(attachment, null);
		}

		/// <summary>
		/// Attempts to convert a native attachment to an <see cref="AgentAttachment"/>. Returns null if the conversion is not possible.
		/// </summary>
		/// <param name="attachment">The native attachment to convert.</param>
		/// <param name="source">The source of the attachment, if applicable.</param>
		/// <returns>The converted <see cref="AgentAttachment"/>, or null if the conversion is not possible.</returns>
		public static AgentAttachment? TryConvertFromNativeAttachment(IAttachment? attachment, object? source)
		{
			switch (attachment)
			{
				case IImageAttachment imageAttachment:
					return new AgentAttachment
					{
						Type = AgentAttachmentType.Image,
						Url = imageAttachment.Url,
						Source = source ?? attachment
					};
				case IAudioAttachment audioAttachment:
					return new AgentAttachment
					{
						Type = AgentAttachmentType.Audio,
						Url = audioAttachment.Url,
						Source = source ?? attachment
					};
				case IVideoAttachment videoAttachment:
					return new AgentAttachment
					{
						Type = AgentAttachmentType.Video,
						Url = videoAttachment.Url,
						Source = source ?? attachment
					};
			}

			return null;
		}
	}
}
