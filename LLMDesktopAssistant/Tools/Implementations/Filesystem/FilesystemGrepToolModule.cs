using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils.Files;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	[ToolModule]
	public class FilesystemGrepToolModule : ToolModule
	{
		private readonly FileAccessService _fileAccess;

		public FilesystemGrepToolModule(FileAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(Grep,
				new ToolInitializationInfo
				{
					Name = "fs-grep",
					Description = """
						Searches for pattern in files using regex.
						Use this tool with care, as it can be slow and resource-intensive.
						""",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.DirectoryRead | ToolBehaviour.FileRead
				});
		}

		public ReactiveToolResult Grep(
			[Description("The regex pattern to search for.")]
			string pattern,
			[Description("The path where to search, can be file or directory path.")]
			string path,
			[Description("The file extensions to allow in search. Examples: '.cs', '.txt' etc.")]
			string[]? allowedExtensions,
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
			try
			{
				var workingDirectory = _fileAccess.GetWorkingDirectory();
				var fullPath = _fileAccess.AccessPath(path);
				var regexIgnoreCaseOptions = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
				var regex = new Regex(pattern, regexIgnoreCaseOptions | RegexOptions.Compiled);

				var filesToSearch = new List<string>();
				var results = new List<string>();
				int totalFilesMatched = 0;

				var result = new ReactiveToolResult();

				bool fileExists = File.Exists(fullPath);
				bool dirExists = Directory.Exists(fullPath);

				if (!fileExists && !dirExists)
					return ReactiveToolResult.CreateError("File or directory not found.");

				Task.Run(() =>
				{
					try
					{
						result.StatusIcon = Material.Icons.MaterialIconKind.FileMultiple;
						result.StatusTitle = LocalizationManager.LocalizeStatic("fs-grep_collecting_files");

						if (fileExists)
						{
							filesToSearch.Add(fullPath);
						}
						else if (dirExists)
						{
							var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
							var allFiles = Directory.GetFiles(fullPath, "*", searchOption);

							foreach (var file in allFiles)
							{
								if (allowedExtensions != null && allowedExtensions.Length > 0)
								{
									var ext = Path.GetExtension(file);
									if (!allowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
										continue;
								}
								/*
								var name = Path.GetFileName(file);
								if (regex.IsMatch(name))
								{
									var relativePath = Path.GetRelativePath(workingDirectory, file);
									results.Add($"[FILE] {relativePath}");
								}
								*/
								try
								{
									// Skip binary files automatically
									if (!FileUtils.IsBinaryFile(file))
									{
										filesToSearch.Add(file);
										result.StatusTitle = string.Format(
											LocalizationManager.LocalizeStatic("fs-grep_collecting_files_count"),
											filesToSearch.Count);
									}
								}
								catch (Exception ex)
								{
									results.Add($"Error checking file {file}: {ex.Message}");
								}
							}
						}

						result.StatusIcon = Material.Icons.MaterialIconKind.FileSearch;
						result.StatusTitle = LocalizationManager.LocalizeStatic("fs-grep_scanning_files");
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
										return m.Substring(0, maxLineLength) + $"...truncated, {m.Length - maxLineLength} more characters";
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

						result.StatusIcon = Material.Icons.MaterialIconKind.FileCheck;
						result.StatusTitle = string.Format(LocalizationManager.LocalizeStatic("fs-grep_completed"), totalFilesMatched);
						result.Complete(true);
					}
					catch
					{
						result.StatusIcon = null;
						result.StatusTitle = null;
						result.Complete(false);
					}
				}, cancellationToken);

				return result;
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Grep error: {ex.Message}");
			}
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
