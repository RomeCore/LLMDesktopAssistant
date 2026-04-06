namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Defines the contract for an attachment description service.
	/// </summary>
	public interface IAttachmentDescriptionService
	{
		/// <summary>
		/// Describes the attachment based on its URL.
		/// Returns the semantic description of the attachment (e.g. for image returns "image of a tabby cat").
		/// </summary>
		/// <param name="url">The URL of the attachment.</param>
		/// <param name="cancellationToken">Token to cancel operation.</param>
		/// <returns>Semantic description of the attachment.</returns>
		Task<string> DescribeAttachmentAsync(string url, CancellationToken cancellationToken = default);
	}
}