namespace LLMDesktopAssistant.Tools.Consents
{
	/// <summary>
	/// Represents the result of a user's consent to use a tool.
	/// </summary>
	public class ToolConsentResult
	{
		/// <summary>
		/// Gets or sets whether the consent for using the tool has been accepted.
		/// </summary>
		public required bool IsApproved { get; init; }

		/// <summary>
		/// Gets or sets the memorization setting for the tool approval.
		/// </summary>
		public required ToolApprovalMemorization Memorization { get; init; }

		/// <summary>
		/// Gets or sets additional notes related to the consent decision.
		/// For accepted tools, this can be used to append some user notes to the end of tool result.
		/// For rejected tools, this can be used to provide a reason for rejection.
		/// </summary>
		public required string? Notes { get; init; }
	}
}