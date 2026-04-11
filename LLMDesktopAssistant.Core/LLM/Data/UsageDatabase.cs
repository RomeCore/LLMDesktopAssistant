using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using LLMDesktopAssistant.Core.LLM.Data.Models;

namespace LLMDesktopAssistant.Core.LLM.Data
{
	/// <summary>
	/// Manages the database for storing and retrieving usage statistics data.
	/// </summary>
	public class UsageDatabase : IDisposable
	{
		public ILiteDatabase Database { get; }
		public ILiteCollection<UsageRecordModel> UsageRecords { get; }

		public UsageDatabase(string path)
		{
			if (Path.GetDirectoryName(path) is string dir)
				Directory.CreateDirectory(dir);
			Database = new LiteDatabase(path);

			UsageRecords = Database.GetCollection<UsageRecordModel>();

			// Create indexes for efficient querying
			UsageRecords.EnsureIndex(x => x.Timestamp);
			UsageRecords.EnsureIndex(x => x.Model);
			UsageRecords.EnsureIndex(x => x.Success);
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