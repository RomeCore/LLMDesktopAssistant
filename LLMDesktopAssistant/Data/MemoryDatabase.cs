using LiteDB;
using LLMDesktopAssistant.Data.MemoryModels;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Embeddings.Database;

namespace LLMDesktopAssistant.Data
{
	public class MemoryDatabase : Disposable
	{
		public ILiteDatabase Database { get; }
		public ILiteCollection<MemoryFactModel> Facts { get; }
		public ILiteCollection<MemoryFactIndexModel> FactIndexes { get; }
		public ILiteCollection<MemoryLogModel> Logs { get; }
		public ILiteCollection<MemoryLogIndexModel> LogIndexes { get; }

		public SemanticDatabase SemanticDatabase { get; }
		public SemanticSector<int> FactSector { get; }

		public MemoryDatabase(string name, LLModel embedModel, string? homeMemoryDirectory = null)
		{
			homeMemoryDirectory ??= Directories.Memory;
			var dataPath = Path.Combine(homeMemoryDirectory, name);
			Directory.CreateDirectory(dataPath);
			var liteDbPath = Path.Combine(dataPath, "data.db");

			Database = new LiteDatabase(liteDbPath);

			Facts = Database.GetCollection<MemoryFactModel>("Facts");
			FactIndexes = Database.GetCollection<MemoryFactIndexModel>("FactIndexes");
			Logs = Database.GetCollection<MemoryLogModel>("Logs");
			LogIndexes = Database.GetCollection<MemoryLogIndexModel>("LogIndexes");

			FactIndexes.EnsureIndex(f => f.FactId);
			FactIndexes.EnsureIndex(f => f.Token);
			Facts.EnsureIndex(f => f.Status);
			LogIndexes.EnsureIndex(l => l.LogId);
			LogIndexes.EnsureIndex(l => l.Token);
			Logs.EnsureIndex(l => l.Status);

			SemanticDatabase = new SemanticDatabase(dataPath, embedModel);
			FactSector = SemanticDatabase.CreateSector<int>("facts", new SemanticSectorProperties<int>
			{
				InputGetter = id => Facts.FindById(id)?.Text ?? string.Empty
			});
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				Database.Dispose();
		}
	}
}
