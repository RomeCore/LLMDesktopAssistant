using System.Text;

namespace LLMDesktopAssistant.Utils.Files
{
	/// <summary>
	/// Represents the diff hunk group of lines, computed by <see cref="UnifiedDiff"/>.
	/// </summary>
	public readonly struct HunkGroup
	{
		public required readonly int OldStart { get; init; }

		public required readonly int OldCount { get; init; }

		public required readonly int NewStart { get; init; }

		public required readonly int NewCount { get; init; }

		public required readonly List<HunkLine> Lines { get; init; }

		/// <summary>
		/// Returns the unified diff for this group, including header (@@ ... @@). <br/>
		/// The output format (unified diff):
		/// <code>
		/// @@ -1,2 +3,4 @@
		///  Some line before change...
		/// -Removed line
		/// +Added line
		///  Some context line after change...
		/// </code>
		/// </summary>
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
					sb.Append(line.ToString());
				else
					sb.AppendLine(line.ToString());
			}
			return sb.ToString();
		}
	}
}