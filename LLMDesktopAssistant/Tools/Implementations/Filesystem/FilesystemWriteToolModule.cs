using System.ComponentModel;
using System.Text;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils.Files;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	/// <summary>
	/// Tool module providing file write operations with integrated diff output.
	/// When overwriting an existing file, it computes and shows a line-based diff
	/// of the changes using a pure C# LCS-based algorithm (no git dependency).
	/// </summary>
	[ToolModule]
	public class FilesystemWriteToolModule : ToolModule
	{
		private readonly FileAccessService _fileAccess;

		public FilesystemWriteToolModule(FileAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(WriteFile,
				new ToolInitializationInfo
				{
					Name = "fs-write_file",
					Description = "Writes text content to a file inside working directory.",
					Category = "filesystem",
					AskForConfirmation = true
				});
		}

		public ReactiveToolResult WriteFile(
			string path,
			string content,
			bool append = false)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var fileName = Path.GetFileName(fullPath);
				var dir = Path.GetDirectoryName(fullPath);

				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir!);

				var fileExisted = File.Exists(fullPath);
				string? oldContent = null;

				// Capture old content before overwriting (for diff)
				if (!append && fileExisted)
				{
					try { oldContent = File.ReadAllText(fullPath); }
					catch { /* Best-effort: skip diff if unreadable */ }
				}

				if (append)
					File.AppendAllText(fullPath, content);
				else
					File.WriteAllText(fullPath, content);

				var fileInfo = new FileInfo(fullPath);
				var size = FileUtils.BytesToDisplaySize(fileInfo.Length);

				var output = new StringBuilder();
				output.AppendLine($"File: {path}");
				output.AppendLine($"Operation: {(append ? "Append" : fileExisted ? "Overwrite" : "Write")}");
				output.AppendLine($"New size: {fileInfo.Length} bytes ~ ({size})");

				// Compute and show diff for overwritten files
				if (!append && fileExisted && oldContent != null)
				{
					var diff = UnifiedDiff.Compute(path, oldContent, content);
					if (diff != null)
					{
						output.AppendLine();
						output.AppendLine("Changes:");
						output.AppendLine(diff);
					}
				}

				var result = new ReactiveToolResult
				{
					StatusIcon = fileExisted ?
						(append ? Material.Icons.MaterialIconKind.FileEdit : Material.Icons.MaterialIconKind.FileCheck) :
						Material.Icons.MaterialIconKind.FilePlus,
					StatusTitle = $"**{fileName}**",
					ResultContent = output.ToString()
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error writing file: {ex.Message}");
			}
		}
	}

	/// <summary>
	/// Pure C# unified diff generator using LCS (Longest Common Subsequence).
	/// Produces output similar to `git diff --no-color` — no external dependencies.
	/// </summary>
	internal static class UnifiedDiff
	{
		public static string? Compute(string path, string oldText, string newText)
		{
			if (oldText == newText)
				return null;

			var oldLines = SplitLines(oldText);
			var newLines = SplitLines(newText);

			// Build the LCS table (Wagner-Fischer)
			int m = oldLines.Length;
			int n = newLines.Length;
			var lcs = new int[m + 1, n + 1];

			for (int i = 1; i <= m; i++)
			{
				for (int j = 1; j <= n; j++)
				{
					lcs[i, j] = oldLines[i - 1] == newLines[j - 1]
						? lcs[i - 1, j - 1] + 1
						: Math.Max(lcs[i - 1, j], lcs[i, j - 1]);
				}
			}

			// Backtrack to build edit operations
			var ops = new List<(int oldLine, int newLine, char kind, string text)>();
			int oi = m, ni = n;

			while (oi > 0 || ni > 0)
			{
				if (oi > 0 && ni > 0 && oldLines[oi - 1] == newLines[ni - 1])
				{
					ops.Add((oi - 1, ni - 1, ' ', oldLines[oi - 1]));
					oi--;
					ni--;
				}
				else if (ni > 0 && (oi == 0 || lcs[oi, ni - 1] >= lcs[oi - 1, ni]))
				{
					ops.Add((oi > 0 ? oi - 1 : -1, ni - 1, '+', newLines[ni - 1]));
					ni--;
				}
				else if (oi > 0)
				{
					ops.Add((oi - 1, ni > 0 ? ni - 1 : -1, '-', oldLines[oi - 1]));
					oi--;
				}
			}

			ops.Reverse();

			// Group into hunks with context
			const int contextLines = 3;
			var sb = new StringBuilder();

			int lastOld = -1, lastNew = -1;
			int hunkStart = 0;
			var hunkLines = new List<(char kind, string text)>();

			void FlushHunk()
			{
				if (hunkLines.Count == 0)
					return;

				int oldStart = hunkStart + 1;
				int newStart = hunkStart + 1;
				int oldCount = 0, newCount = 0;

				// Count actual changes in the hunk (excluding context)
				foreach (var (kind, _) in hunkLines)
				{
					if (kind == '-') oldCount++;
					else if (kind == '+') newCount++;
					else { oldCount++; newCount++; }
				}

				sb.AppendLine($"@@ -{oldStart},{oldCount} +{newStart},{newCount} @@");

				foreach (var (kind, text) in hunkLines)
					sb.AppendLine($"{kind}{text}");

				sb.AppendLine();
			}

			foreach (var (oldLine, newLine, kind, text) in ops)
			{
				if (kind == ' ')
				{
					// Context line
					if (hunkLines.Count > 0)
					{
						hunkLines.Add((kind, text));
						// Count context lines after last change
						int contextCount = 0;
						for (int i = hunkLines.Count - 1; i >= 0; i--)
						{
							if (hunkLines[i].kind is '-' or '+')
							{
								contextCount = 0;
								break;
							}
							contextCount++;
							if (contextCount > contextLines)
								break;
						}

						if (contextCount > contextLines)
						{
							// Remove extra context lines from end
							int remove = contextCount - contextLines;
							hunkLines.RemoveRange(hunkLines.Count - remove, remove);
							FlushHunk();
							hunkLines.Clear();
							// Add trailing context as leading context for next hunk
							// (simplified: just reset)
						}
					}
					lastOld = oldLine;
					lastNew = newLine;
				}
				else
				{
					// Change line
					if (hunkLines.Count == 0)
					{
						hunkStart = Math.Max(0, oldLine - contextLines);
						// Add leading context
						int ctxStart = Math.Max(0, oldLine - contextLines);
						int ctxEnd = oldLine;
						for (int i = ctxStart; i < ctxEnd; i++)
							hunkLines.Add((' ', oldLines[i]));
					}
					hunkLines.Add((kind, text));
					lastOld = oldLine;
					lastNew = newLine;
				}
			}

			if (hunkLines.Count > 0)
				FlushHunk();

			return sb.Length > 0 ? sb.ToString().TrimEnd() : null;
		}

		private static string[] SplitLines(string text)
		{
			if (string.IsNullOrEmpty(text))
				return [];
			return text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
		}
	}
}
