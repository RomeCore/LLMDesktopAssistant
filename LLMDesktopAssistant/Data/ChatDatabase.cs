using LiteDB;
using LLMDesktopAssistant.Data.ChatModels;

namespace LLMDesktopAssistant.Data
{
	/// <summary>
	/// Manages the database for storing and retrieving conversation data.
	/// </summary>
	public class ChatDatabase : IDisposable
	{
		public ILiteDatabase Database { get; }
		public ILiteCollection<ChatModel> Chats { get; }
		public ILiteCollection<MessageNodeModel> MessageNodes { get; }
		public ILiteCollection<MessageModel> Messages { get; }
		public ILiteCollection<AttachmentModel> Attachments { get; }
		public ILiteCollection<ToolCallModel> ToolCalls { get; }
		public ILiteCollection<ChatContextTabViewDataModel> ChatContextTabViewModels { get; }
		public ILiteCollection<AdditionalMessageViewDataModel> AdditionalMessageViewModels { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatDatabase"/> class.
		/// </summary>
		/// <param name="path">The path to the database file. Or the "Memory=true;" if you want to use in-memory database.</param>
		public ChatDatabase(string path)
		{
			if (path != "Filename=:memory:" && Path.GetDirectoryName(path) is string dir)
				Directory.CreateDirectory(dir);
			Database = new LiteDatabase(path);

			Chats = Database.GetCollection<ChatModel>();
			MessageNodes = Database.GetCollection<MessageNodeModel>();
			Messages = Database.GetCollection<MessageModel>();
			Attachments = Database.GetCollection<AttachmentModel>();
			ToolCalls = Database.GetCollection<ToolCallModel>();
			ChatContextTabViewModels = Database.GetCollection<ChatContextTabViewDataModel>();
			AdditionalMessageViewModels = Database.GetCollection<AdditionalMessageViewDataModel>();

			MessageNodes.EnsureIndex(x => x.ParentId);
			MessageNodes.EnsureIndex(x => x.SelectedNodeId);
			Attachments.EnsureIndex(x => x.ParentId);
			ToolCalls.EnsureIndex(x => x.MessageId);
			ToolCalls.EnsureIndex(x => x.ToolCallId);
			ChatContextTabViewModels.EnsureIndex(x => x.ChatId);
			AdditionalMessageViewModels.EnsureIndex(x => x.MessageId);
		}

		/// <summary>
		/// Disposes the database connection.
		/// </summary>
		public void Dispose()
		{
			Database?.Dispose();
		}
	}
}