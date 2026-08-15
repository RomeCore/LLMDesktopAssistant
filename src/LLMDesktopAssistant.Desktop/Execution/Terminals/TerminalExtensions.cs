using System.Text;
using XTerm;

namespace LLMDesktopAssistant.Desktop.Execution.Terminals
{
	public static class TerminalExtensions
	{
		extension(Terminal terminal)
		{
			public string GetBufferContents()
			{
				var buffer = terminal.Buffer;
				var sb = new StringBuilder();
				for (int i = 0; i < buffer.Lines.Length; i++)
				{
					var line = buffer.Lines[i];
					if (line == null)
					{
						if (i < buffer.Lines.Length - 1)
							sb.AppendLine();
						continue;
					}
					for (int j = 0; j < line.Length; j++)
					{
						var cell = line[j];
						sb.Append(cell.Content);
					}
					if (i < buffer.Lines.Length - 1)
						sb.AppendLine();
				}
				return sb.ToString();
			}

			public List<string> GetBufferLines()
			{
				var buffer = terminal.Buffer;
				var result = new List<string>();
				var sb = new StringBuilder();
				for (int i = 0; i < buffer.Lines.Length; i++)
				{
					var line = buffer.Lines[i];
					if (line == null)
					{
						if (i < buffer.Lines.Length - 1)
						{
							result.Add(sb.ToString());
							sb.Clear();
						}
						continue;
					}
					for (int j = 0; j < line.Length; j++)
					{
						var cell = line[j];
						sb.Append(cell.Content);
					}
					if (i < buffer.Lines.Length - 1)
					{
						result.Add(sb.ToString());
						sb.Clear();
					}
				}
				return result;
			}
		}
	}
}
