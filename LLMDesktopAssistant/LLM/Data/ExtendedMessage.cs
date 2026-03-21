using System.Collections.ObjectModel;
using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM.Data
{
	public class ExtendedMessage
	{
		/// <summary>
		/// The message that being wrapped.
		/// </summary>
		public required IMessage Message { get; init; }

		/// <summary>
		/// The collection of tool messages associated with the message.
		/// </summary>
		public ObservableCollection<ExtendedMessage> ToolMessages { get; init; } = [];


	}
}
