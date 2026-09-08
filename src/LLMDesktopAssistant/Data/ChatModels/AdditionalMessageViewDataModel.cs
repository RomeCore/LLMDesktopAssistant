using LiteDB;
using LLMDesktopAssistant.LLM.MVVM.Additional;

namespace LLMDesktopAssistant.Data.ChatModels
{
	/// <summary>
	/// The additional view model that can be applied to a message or a tool call.
	/// </summary>
	public class AdditionalMessageViewDataModel
	{
		/// <summary>
		/// The unique identifier for the message view data model.
		/// </summary>
		[BsonId]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets a value indicating kind of parent for this additional view model.
		/// </summary>
		public VMParentKind ParentKind { get; set; }

		/// <summary>
		/// Gets or sets the message or tool call ID that this additional view model belongs to.
		/// </summary>
		public int ParentId { get; set; }

		/// <summary>
		/// The additional view model associated with the message.
		/// </summary>
		public AdditionalMessageViewModel ViewModel
		{
			get => field ??= new();
			set => field = value;
		}
	}
}
