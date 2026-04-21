using LiteDB;
using LLMDesktopAssistant.LLM.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Data.ChatModels
{
	public class AdditionalMessageViewDataModel
	{
		/// <summary>
		/// The unique identifier for the message view data model.
		/// </summary>
		[BsonId]
		public int Id { get; set; }

		/// <summary>
		/// The unique identifier for the message associated with this view data model.
		/// </summary>
		public int MessageId { get; set; }

		/// <summary>
		/// The additional view model associated with the message.
		/// </summary>
		public AdditionalMessageViewModel ViewModel { get; set; } = new AdditionalMessageViewModel();
	}
}