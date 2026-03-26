using System.Collections.ObjectModel;
using LLMDesktopAssistant.ToolModules;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents a chat session.
	/// </summary>
	public class Chat(IServiceProvider services) : Disposable
	{
		/// <summary>
		/// Gets the service provider used to resolve dependencies.
		/// </summary>
		public IServiceProvider Services { get; } = services;

		/// <summary>
		/// Gets or sets the unique identifier for the chat session. Used mostly for database purposes.
		/// </summary>
		public int ChatId { get; set; }

		/// <summary>
		/// The collection of messages in the chat session.
		/// </summary>
		public RangeObservableCollection<BranchedMessage> Messages { get; } = [];

		/// <summary>
		/// Gets or sets the system prompt for the chat session. Used to provide context and instructions to the model.
		/// </summary>
		public string? SystemPrompt { get; set; }

		/// <summary>
		/// Gets or sets the list of tool modules that are available for use in the chat session.
		/// </summary>
		public List<ToolModule> AdditionalToolModules { get; set; } = [];

		/// <summary>
		/// The list of properties associated with the chat session. These can include additional settings or configurations that affect the behavior of the chat session.
		/// </summary>
		public ObservableCollection<ChatProperty> Properties { get; } = [];


		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				foreach (var message in Messages)
				{
					message.Dispose();
				}
			}
		}
	}
}