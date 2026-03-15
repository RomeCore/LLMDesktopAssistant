using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using LLMDesktopAssistant.LLM.Conversations.Models;

namespace LLMDesktopAssistant.LLM.Conversations
{
	public class Database
	{
		private readonly LiteDatabase _database;

		public ILiteCollection<Conversation> Conversations { get; }
		public ILiteCollection<MessageRootNode> MessageRootNodes { get; }
		public ILiteCollection<MessageNode> MessageNodes { get; }
		public ILiteCollection<Message> Messages { get; }
		public ILiteCollection<Attachment> Attachments { get; }
		public ILiteCollection<ToolCall> ToolCalls { get; }

		public Database(string path)
		{
			if (Path.GetDirectoryName(path) is string dir)
				Directory.CreateDirectory(dir);
			_database = new LiteDatabase(path);

			Conversations = _database.GetCollection<Conversation>();
			MessageRootNodes = _database.GetCollection<MessageRootNode>();
			MessageNodes = _database.GetCollection<MessageNode>();
			Messages = _database.GetCollection<Message>();
			Attachments = _database.GetCollection<Attachment>();
			ToolCalls = _database.GetCollection<ToolCall>();

			Attachments.EnsureIndex(x => x.MessageId);
			ToolCalls.EnsureIndex(x => x.MessageId);
		}
	}
}