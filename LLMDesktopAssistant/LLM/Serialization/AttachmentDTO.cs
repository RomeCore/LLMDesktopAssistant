using RCLargeLanguageModels.Messages.Attachments;

namespace LLMDesktopAssistant.LLM.Serialization
{
	public class AttachmentDTO
	{
		public AttachmentDTO()
		{
		}

		public static AttachmentDTO ConvertFrom(IAttachment attachment)
		{
			return new AttachmentDTO();
		}

		public IAttachment ConvertBack()
		{
			throw new NotImplementedException();
		}
	}
}