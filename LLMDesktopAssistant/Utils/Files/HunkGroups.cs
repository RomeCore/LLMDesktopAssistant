using System.Collections;
using System.Text;

namespace LLMDesktopAssistant.Utils.Files
{
	/// <summary>
	/// Represents the diff hunk groups collection, computed by <see cref="UnifiedDiff"/>.
	/// </summary>
	public readonly struct HunkGroups : IEnumerable<HunkGroup>
	{
		public required readonly List<HunkGroup> Groups { get; init; }

		public readonly bool HasGroups => Groups?.Count > 0;

		public IEnumerator<HunkGroup> GetEnumerator()
		{
			return ((IEnumerable<HunkGroup>)Groups).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)Groups).GetEnumerator();
		}

		/// <summary>
		/// Returns the concatenated strings of <see cref="Groups"/>, delimited by double newlines. <br/>
		/// The output format (unified diff):
		/// <code>
		/// @@ -1,2 +3,4 @@
		///  Some line before change...
		/// -Removed line
		/// +Added line
		///  Some context line after change...
		/// 
		/// @@ -5,6 +7,8 @@
		///  The next group goes here...
		/// </code>
		/// </summary>
		public override string ToString()
		{
			if (Groups is null || Groups.Count == 0)
				return string.Empty;
			if (Groups.Count == 1)
				return Groups[0].ToString();

			var sb = new StringBuilder();
			for (int i = 0; i < Groups.Count; i++)
			{
				sb.Append(Groups[i].ToString());
				if (i < Groups.Count - 1)
					sb.AppendLine().AppendLine();
			}
			return sb.ToString();
		}

		/// <summary>
		/// Returns the total number of lines that have been removed and added across all hunk groups.
		/// </summary>
		public (int Removed, int Added) GetChangeCounts()
		{
			int removed = 0, added = 0;

			foreach (var group in Groups)
			{
				foreach (var line in group.Lines)
				{
					if (line.Kind == '-')
						removed++;
					else if (line.Kind == '+')
						added++;
				}
			}

			return (removed, added);
		}
	}
}