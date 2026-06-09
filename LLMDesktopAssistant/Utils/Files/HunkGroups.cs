using System.Collections;
using System.Text;

namespace LLMDesktopAssistant.Utils.Files
{
	public readonly struct HunkGroups : IEnumerable<HunkGroup>
	{
		public required readonly List<HunkGroup> Groups { get; init; }

		public IEnumerator<HunkGroup> GetEnumerator()
		{
			return ((IEnumerable<HunkGroup>)Groups).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)Groups).GetEnumerator();
		}

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
	}
}