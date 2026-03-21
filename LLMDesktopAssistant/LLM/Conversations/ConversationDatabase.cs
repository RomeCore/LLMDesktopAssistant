using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using LiteDB;
using LLMDesktopAssistant.LLM.Conversations.Models;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Agents;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.Conversations
{
	/// <summary>
	/// Manages the database for storing and retrieving conversation data.
	/// </summary>
	public class ConversationDatabase
	{
		public ILiteDatabase Database { get; }
		public ILiteCollection<ConversationModel> Conversations { get; }
		public ILiteCollection<MessageNodeModel> MessageNodes { get; }
		public ILiteCollection<MessageModel> Messages { get; }
		public ILiteCollection<AttachmentModel> Attachments { get; }
		public ILiteCollection<ToolCallModel> ToolCalls { get; }

		public ConversationDatabase(string path)
		{
			if (Path.GetDirectoryName(path) is string dir)
				Directory.CreateDirectory(dir);
			Database = new LiteDatabase(path);

			Conversations = Database.GetCollection<ConversationModel>();
			MessageNodes = Database.GetCollection<MessageNodeModel>();
			Messages = Database.GetCollection<MessageModel>();
			Attachments = Database.GetCollection<AttachmentModel>();
			ToolCalls = Database.GetCollection<ToolCallModel>();

			MessageNodes.EnsureIndex(x => x.ParentId);
			MessageNodes.EnsureIndex(x => x.SelectedNodeId);
			Attachments.EnsureIndex(x => x.MessageId);
			ToolCalls.EnsureIndex(x => x.MessageId);
			ToolCalls.EnsureIndex(x => x.ToolCallId);
		}
	}
}