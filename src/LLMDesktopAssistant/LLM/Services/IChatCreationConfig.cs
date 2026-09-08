using LLMDesktopAssistant.Data;

namespace LLMDesktopAssistant.LLM.Services
{
	public interface IChatCreationConfig
	{
		/// <summary>
		/// The chat ID that will be created.
		/// </summary>
		int ChatId { get; set; }

		/// <summary>
		/// The timestamp when the chat was created.
		/// </summary>
		DateTime CreatedAt { get; set; }

		/// <summary>
		/// The database where the chat instance will be stored.
		/// </summary>
		/// <remarks>
		/// This database can be vary from <see cref="ChatDatabase"/> that you can get from services.
		/// </remarks>
		ChatDatabase Database { get; set; }
	}
}
