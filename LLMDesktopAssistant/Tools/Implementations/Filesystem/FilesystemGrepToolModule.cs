using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils.Files;
using Material.Icons;
using ModelContextProtocol.Protocol;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	[ToolModule]
	public class FilesystemGrepToolModule : ToolModule
	{
		private readonly WorkingDirectoryAccessService _fileAccess;

		public FilesystemGrepToolModule(WorkingDirectoryAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(Grep, GrepStreaming, GrepPreview,
				new ToolInitializationInfo
				{
					Name = "fs-grep",
					Description = """
						Searches for pattern in files using regex.
						Use this tool with care, as it can be slow and resource-intensive.
						""",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.DirectoryRead | ToolBehaviour.FileRead | ToolBehaviour.AccessOutsideWorkdir
				});
		}

		public StreamingToolArgumentsAnalysisResult GrepStreaming(
			string? path, string? pattern)
		{
			path ??= "?";
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.FileSearch,
				StatusTitle = pattern != null ? $"**{path}** → `{pattern}`" : $"**{path}**"
			};
		}

		public PreviewToolExecutionResult GrepPreview(
			string path, string pattern, [SharedContext] out string fullPath)
		{
			fullPath = _fileAccess.CheckedAccessPath(path, out var isAccessed);
			bool fileExists = File.Exists(fullPath);
			bool dirExists = Directory.Exists(fullPath);

			if (!fileExists && !dirExists)
			{
				new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileSearch,
					StatusTitle = $"**{path}** → `{pattern}`",
					ExpectedBehaviour = !isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = $"File or directory not found: {path}"
				};
			}

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.FileSearch,
				StatusTitle = $"**{path}** → `{pattern}`",
				ExpectedBehaviour = ToolBehaviour.FileRead |
					(dirExists ? ToolBehaviour.DirectoryRead : ToolBehaviour.None) |
					(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
			};
		}

		public ReactiveToolResult Grep(
			[SharedContext] string? fullPath,
			[Description("The path where to search, can be file or directory path.")]
			string path,
			[Description("The .NET regex pattern to search for.")]
			string pattern,
			[Description("The file extensions to allow in search. Examples: [\".cs\", \".txt\"]")]
			string[] allowedExtensions,
			[Description("The maximum count of matched files to return.")]
			int limitFiles = 20,
			[Description("The maximum count of matched lines per file to return.")]
			int limitLinesPerFile = 20,
			[Description("Whether to ignore case when searching for the pattern.")]
			bool ignoreCase = false,
			[Description("Whether to return line numbers with the matched lines.")]
			bool lineNumbers = true,
			[Description("Whether to return non-matched lines instead of matched lines.")]
			bool invert = false,
			[Description("Whether to return only regex matches instead of entire lines.")]
			bool onlyMatching = false,
			[Description("The number of lines to show before each match.")]
			int beforeContext = 0,
			[Description("The number of lines to show after each match.")]
			int afterContext = 0,
			[Description("Whether to search recursively through subdirectories.")]
			bool recursive = true,
			[Description("The maximum length of each line to return.")]
			int maxLineLength = 2000,
			CancellationToken cancellationToken = default)
		{
			var result = new ReactiveToolResult
			{
				StatusIcon = MaterialIconKind.FileSearch,
				StatusTitle = $"**{path}** → `{pattern}`"
			};

			try
			{
				var workingDirectory = _fileAccess.GetWorkingDirectory();
				fullPath ??= _fileAccess.AccessPath(path);
				var regexIgnoreCaseOptions = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
				var regex = new Regex(pattern, regexIgnoreCaseOptions | RegexOptions.Compiled);

				var filesToSearch = new List<string>();
				var results = new List<string>();
				int totalFilesMatched = 0;

				bool fileExists = File.Exists(fullPath);
				bool dirExists = Directory.Exists(fullPath);

				if (!fileExists && !dirExists)
				{
					result.ResultContent = $"File or directory not found: {path}";
					return result.CompleteWithError();
				}

				if (dirExists && (allowedExtensions == null || allowedExtensions.Length == 0))
				{
					result.ResultContent = "Allowed extensions cannot be empty when searching inside directory.";
					return result.CompleteWithError();
				}

				Task.Run(() =>
				{
					try
					{
						result.StatusIcon = MaterialIconKind.FileMultiple;
						result.StatusTitle = $"**{path}** → `{pattern}` " +
							LocalizationManager.LocalizeStatic("fs-grep_collecting_files");

						if (fileExists)
						{
							filesToSearch.Add(fullPath);
						}
						else if (dirExists)
						{
							var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
							var allFiles = Directory.EnumerateFiles(fullPath, "*", searchOption);

							foreach (var file in allFiles)
							{
								cancellationToken.ThrowIfCancellationRequested();

								var ext = Path.GetExtension(file);
								if (!allowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
									continue;

								filesToSearch.Add(file);
								result.StatusTitle = $"**{path}** → `{pattern}` " + string.Format(
									LocalizationManager.LocalizeStatic("fs-grep_collecting_files_count"),
									filesToSearch.Count);
							}
						}

						result.StatusIcon = MaterialIconKind.FileSearch;
						result.StatusTitle = $"**{path}** → `{pattern}` " +
							LocalizationManager.LocalizeStatic("fs-grep_scanning_files");
						result.Progress = 0;
						result.MaxProgress = filesToSearch.Count;

						foreach (var file in filesToSearch)
						{
							result.Progress++;
							cancellationToken.ThrowIfCancellationRequested();

							var relativePath = Path.GetRelativePath(workingDirectory, file);
							var fileMatches = SearchInFile(workingDirectory, file, regex, limitLinesPerFile, invert, onlyMatching,
															lineNumbers, beforeContext, afterContext, cancellationToken);
							if (fileMatches.Count > 0)
							{
								fileMatches = fileMatches.Select(m =>
								{
									if (m.Length > maxLineLength)
										return string.Concat(m.AsSpan(0, maxLineLength), $"...truncated, {m.Length - maxLineLength} more characters");
									return m;
								}).ToList();

								result.ResultContentLines.Add($"\n--- {relativePath} ---");
								result.ResultContentLines.AddRange(fileMatches);

								totalFilesMatched++;
								if (totalFilesMatched >= limitFiles)
									break;
							}
						}

						if (result.ResultContentLines.Count == 0)
							result.ResultContentLines.Add("No matches found.");

						result.StatusIcon = MaterialIconKind.FileCheck;
						result.StatusTitle = $"**{path}** → `{pattern}` " +
							string.Format(LocalizationManager.LocalizeStatic("fs-grep_completed"), totalFilesMatched);
						result.CompleteWithSuccess();
					}
					catch (Exception ex)
					{
						result.ResultContentLines.Add("Error occured: " + ex.Message);
						result.StatusIcon = MaterialIconKind.FileDocumentError;
						result.StatusTitle = $"**{path}** → `{pattern}`";
						result.CompleteWithError();
					}
				}, cancellationToken);
			}
			catch (Exception ex)
			{
				result.ResultContent = $"Grep error: {ex.Message}";
				result.CompleteWithError();
			}

			return result;
		}

		private List<string> SearchInFile(
			string workDir,
			string filePath,
			Regex regex,
			int limitLinesPerFile,
			bool invert,
			bool onlyMatching,
			bool showLineNumbers,
			int beforeContext,
			int afterContext,
			CancellationToken cancellationToken = default)
		{
			string[] lines = File.ReadAllLines(filePath);
			try
			{
				lines = File.ReadAllLines(filePath);
			}
			catch
			{
				var relativePath = Path.GetRelativePath(workDir, filePath);
				return [$"Cannot read file {relativePath}: it may be corrupted or taken by another process."];
			}

			var matches = new List<string>();
			int count = 0;

			for (int i = 0; i < lines.Length; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var line = lines[i];
				var isMatch = regex.IsMatch(line);

				if (invert)
					isMatch = !isMatch;

				if (!isMatch)
					continue;

				// Add context lines before
				if (beforeContext > 0)
				{
					int contextStart = Math.Max(0, i - beforeContext);
					for (int ctx = contextStart; ctx < i; ctx++)
					{
						if (!matches.Contains($"{ctx + 1}: {lines[ctx]}"))
						{
							if (showLineNumbers)
								matches.Add($"{ctx + 1}: {lines[ctx]}");
							else
								matches.Add(lines[ctx]);
						}
					}
				}

				// Add the matching line
				if (onlyMatching)
				{
					var matchesCollection = regex.Matches(line);
					foreach (Match m in matchesCollection)
					{
						if (showLineNumbers)
							matches.Add($"{i + 1}: {m.Value}");
						else
							matches.Add(m.Value);
					}
				}
				else
				{
					if (showLineNumbers)
						matches.Add($"{i + 1}: {line}");
					else
						matches.Add(line);
				}

				// Add context lines after
				if (afterContext > 0)
				{
					int contextEnd = Math.Min(lines.Length, i + afterContext + 1);
					for (int ctx = i + 1; ctx < contextEnd; ctx++)
					{
						if (!matches.Contains($"{ctx + 1}: {lines[ctx]}"))
						{
							if (showLineNumbers)
								matches.Add($"{ctx + 1}: {lines[ctx]}");
							else
								matches.Add(lines[ctx]);
						}
					}
				}

				count++;
				if (count >= limitLinesPerFile)
					break;
			}

			return matches;
		}
	}
}
