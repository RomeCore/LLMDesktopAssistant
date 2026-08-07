using LiteDB;

namespace LLMDesktopAssistant.Data.MemoryModels
{
	public class MemoryLogIndexModel
	{
		[BsonId]
		public int Id { get; set; }

		public int LogId { get; set; }

		public string Token { get; set; } = string.Empty;

		public int Count { get; set; } = 1;
	}
}
