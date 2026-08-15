using LiteDB;

namespace LLMDesktopAssistant.Data.MemoryModels
{
	public class MemoryFactIndexModel
	{
		[BsonId]
		public int Id { get; set; }

		public int FactId { get; set; }

		public string Token { get; set; } = string.Empty;

		public int Count { get; set; } = 1;
	}
}
