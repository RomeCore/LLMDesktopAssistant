using LiteDB;
using LLMDesktopAssistant.LLM.MVVM.ContextTabs;

namespace LLMDesktopAssistant.Data.ChatModels
{
	public class ChatContextTabViewDataModel
	{
		/// <summary>
		/// The unique identifier for the chat tab view data model.
		/// </summary>
		[BsonId]
		public int Id { get; set; }

		/// <summary>
		/// The unique identifier for the chat associated with this view data model.
		/// </summary>
		public int ChatId { get; set; }

		/// <summary>
		/// The additional view model associated with the message.
		/// </summary>
		public ChatContextTabViewModel ViewModel { get; set; } = new ChatContextTabViewModel();
	}
}