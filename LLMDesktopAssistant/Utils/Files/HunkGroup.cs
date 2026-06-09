using System.Text;
using DocumentFormat.OpenXml.InkML;

namespace LLMDesktopAssistant.Utils.Files
{
	public readonly struct HunkGroup
	{
		public required readonly int OldStart { get; init; }

		public required readonly int OldCount { get; init; }

		public required readonly int NewStart { get; init; }

		public required readonly int NewCount { get; init; }

		public required readonly List<HunkLine> Lines { get; init; }

		public override string ToString()
		{
			if (Lines is null || Lines.Count == 0)
				return $"@@ -{OldStart},{OldCount} +{NewStart},{NewCount} @@";
			
			var sb = new StringBuilder();
			sb.AppendLine($"@@ -{OldStart},{OldCount} +{NewStart},{NewCount} @@");
			for (int i = 0; i < Lines.Count; i++)
			{
				var line = Lines[i];
				if (i == Lines.Count - 1)
					sb.Append($"{line.Kind}{line.Content}");
				else
					sb.AppendLine($"{line.Kind}{line.Content}");
			}
			return sb.ToString();
		}
	}
}