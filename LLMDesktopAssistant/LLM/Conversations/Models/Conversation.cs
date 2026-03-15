using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace LLMDesktopAssistant.LLM.Conversations.Models
{
	public sealed class Conversation
	{
		[BsonId]
		public int Id { get; set; }
	}
}