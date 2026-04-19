using System.IO;

namespace LLMDesktopAssistant.Utils.Files
{
	public class FileMetrics
	{
		public required string Name { get; init; }
		public required string FullPath { get; init; }
		public required long Size { get; init; }

		public required FileType Type { get; init; }
		public required bool IsBinary { get; init; }

		public int? LineCount { get; init; }

		public required DateTime Created { get; init; }
		public required DateTime Modified { get; init; }
		public required FileAttributes Attributes { get; init; }
	}
}