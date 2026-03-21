namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents a base class for attachments.
	/// </summary>
	public abstract class Attachment : NotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets the title of the attachment.
		/// </summary>
		public required string Title { get; init; }
	}
}