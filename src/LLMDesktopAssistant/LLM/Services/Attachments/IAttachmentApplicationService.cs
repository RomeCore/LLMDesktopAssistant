using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Additional;

namespace LLMDesktopAssistant.LLM.Services.Attachments
{
	/// <summary>
	/// Defines the contract for an attachment application service.
	/// </summary>
	public interface IAttachmentApplicationService
	{
		/// <summary>
		/// Makes attachment to be applied to the user message.
		/// </summary>
		/// <param name="parameters">The parameters for the attachment application.</param>
		/// <param name="cancellationToken">Token to cancel operation.</param>
		/// <returns>Ready-to-use attachment part.</returns>
		Task<AttachmentMessagePart> ApplyAttachmentAsync(AttachmentApplicationParameters parameters,
			CancellationToken cancellationToken = default);
	}
}