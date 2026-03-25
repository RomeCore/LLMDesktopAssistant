namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents the user's input used for generating a response from the LLM assistant.
	/// </summary>
	public class UserInput
	{
		/// <summary>
		/// Gets or sets the content of the user input. This is typically the text entered by the user in a chat interface.
		/// </summary>
		public required string Content { get; init; }

		/// <summary>
		/// Gets or sets the collection of attachments associated with the user input. These can include images, files, or other types of data. If no attachments are present, this property is an empty collection.
		/// </summary>
		public IEnumerable<Attachment> Attachments { get; init; } = [];
	}
}