using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services.Attachments
{
	/// <summary>
	/// Defines the contract for an attachment application service.
	/// </summary>
	public interface IAttachmentApplicationService
	{
		/// <summary>
		/// Gets the recommended attachment application parameters for a given URI.
		/// </summary>
		/// <param name="uri">The URI of the attachment.</param>
		/// <param name="cancellationToken">Token to cancel operation.</param>
		/// <returns>The recommended attachment application parameters.</returns>
		Task<AttachmentApplicationParameters> GetRecommendedParamatersAsync(Uri uri,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Makes attachment to be applicated to the user message.
		/// </summary>
		/// <param name="parameters">The parameters for the attachment application.</param>
		/// <param name="cancellationToken">Token to cancel operation.</param>
		/// <returns>Ready-to-use attachment.</returns>
		Task<Attachment> ApplicateAttachmentAsync(AttachmentApplicationParameters parameters,
			CancellationToken cancellationToken = default);
	}
}