using System.ComponentModel;
using System.Text;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils.Files;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	/// <summary>
	/// Tool module that provides glob-based file and directory searching.
	/// Uses Microsoft.Extensions.FileSystemGlobbing under the hood.
	/// </summary>
	[ToolModule]
	public class FilesystemGlobToolModule : ToolModule
	{
		private readonly FileAccessService _fileAccess;

		public FilesystemGlobToolModule(FileAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(Glob,
				new ToolInitializationInfo
				{
					Name = "fs-glob",
					Description = """
						Finds files and directories matching a glob pattern.
						Glob patterns are file matching patterns similar to those used in bash and .gitignore files.

						Supported pattern syntax:
						- `*`       Matches any characters except directory separator
						- `**`      Matches any characters including directory separators (recursive)
						- `?`       Matches any single character except directory separator
						- `[abc]`   Matches any character in the set
						- `[!abc]`  Matches any character not in the set
						- `{a,b}`   Matches either pattern 'a' or pattern 'b'

						Examples:
						- `**/*.cs`               - all C# files recursively
						- `*.txt`                 - all text files in root only
						- `src/**/test*`          - files starting with 'test' in any subdirectory of 'src'
						- `*.{cs,py,js}`          - C#, Python or JavaScript files
						- `[!A-Z]*.md`            - Markdown files not starting with uppercase letter
						- `{include,src}/**/*.h`  - header files in 'include' or 'src' directories
						""",
					Category = "filesystem",
					AskForConfirmation = false
				});
		}

		public ReactiveToolResult Glob(
			[Description("The glob pattern to search for, e.g. '**/*.cs' or '*.txt'")]
			string pattern,
			[Description("The starting directory path. If empty or '.', uses the current working directory.")]
			string path = ".",
			[Description("The maximum number of results to return. Use 0 for unlimited.")]
			int limit = 100,
			[Description("Include files in results.")]
			bool files = true,
			[Description("Include directories in results.")]
			bool directories = false,
			[Description("Whether to show hidden files and directories (those starting with dot).")]
			bool showHidden = false,
			[Description("Whether to return paths relative to the working directory.")]
			bool relativePaths = true,
			CancellationToken cancellationToken = default)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var workingDirectory = _fileAccess.GetWorkingDirectory();

				var result = new ReactiveToolResult();

				Task.Run(() =>
				{
					try
					{
						result.StatusIcon = Material.Icons.MaterialIconKind.FolderSearch;
						result.StatusTitle = string.Format(LocalizationManager.LocalizeStatic("fs-glob_searching"), pattern);

						var matcher = new Matcher();
						matcher.AddInclude(pattern);

						var matchingFiles = new List<string>();
						var matchingDirectories = new List<string>();

						// Search files via Matcher
						if (files)
						{
							var globResult = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(fullPath)));

							foreach (var file in globResult.Files)
							{
								cancellationToken.ThrowIfCancellationRequested();

								var filePath = Path.GetFullPath(Path.Combine(fullPath, file.Path));

								if (!showHidden)
								{
									var fileName = Path.GetFileName(filePath);
									if (fileName.StartsWith("."))
										continue;
								}

								try
								{
									var metrics = FileUtils.GetFileMetrics(filePath);
									var lines = metrics.LineCount != null ? $"{metrics.LineCount} lines" : "binary";
									var displayPath = relativePaths
										? Path.GetRelativePath(workingDirectory, filePath)
										: filePath;

									matchingFiles.Add($"[FILE] {displayPath} ({FileUtils.BytesToDisplaySize(metrics.Size)}, {lines}, {metrics.Modified:yyyy-MM-dd HH:mm})");

									result.StatusTitle = string.Format(LocalizationManager.LocalizeStatic("fs-glob_found_count"), matchingFiles.Count + matchingDirectories.Count);

									if (limit > 0 && matchingFiles.Count + matchingDirectories.Count >= limit)
										break;
								}
								catch
								{
									// skip inaccessible files
								}
							}
						}

						// Search directories via Directory.GetDirectories + Matcher.Match
						if (directories && (limit == 0 || matchingFiles.Count + matchingDirectories.Count < limit))
						{
							var allDirs = Directory.GetDirectories(fullPath, "*", SearchOption.AllDirectories);

							foreach (var dir in allDirs)
							{
								cancellationToken.ThrowIfCancellationRequested();

								if (!showHidden)
								{
									var dirName = Path.GetFileName(dir);
									if (dirName.StartsWith("."))
										continue;
								}

								var relDir = Path.GetRelativePath(fullPath, dir);

								// Check if directory matches the glob pattern using Matcher
								var dirMatcher = new Matcher();
								dirMatcher.AddInclude(pattern);
								var dirMatchResult = dirMatcher.Match(fullPath, relDir);
								if (!dirMatchResult.HasMatches)
									continue;

								try
								{
									var dirInfo = new DirectoryInfo(dir);
									int items;
									try
									{
										items = dirInfo.GetFileSystemInfos().Length;
									}
									catch
									{
										items = 0;
									}

									var displayPath = relativePaths
										? Path.GetRelativePath(workingDirectory, dir)
										: dir;

									matchingDirectories.Add($"[DIR] {displayPath} ({items} items, {dirInfo.LastWriteTime:yyyy-MM-dd HH:mm})");

									result.StatusTitle = string.Format(LocalizationManager.LocalizeStatic("fs-glob_found_count"), matchingFiles.Count + matchingDirectories.Count);

									if (limit > 0 && matchingFiles.Count + matchingDirectories.Count >= limit)
										break;
								}
								catch
								{
									// skip inaccessible directories
								}
							}
						}

						// Build output
						var totalFiles = matchingFiles.Count;
						var totalDirs = matchingDirectories.Count;
						var total = totalFiles + totalDirs;

						var sb = new StringBuilder();
						sb.AppendLine("[GLOB RESULTS]");
						sb.AppendLine($"Pattern: {pattern}");
						sb.AppendLine($"Path: {path}");
						sb.AppendLine($"Found: {total} item(s)");
						if (limit > 0 && total >= limit)
							sb.AppendLine($"(Limited to {limit} results)");
						sb.AppendLine();

						// Directories first, then files
						foreach (var dir in matchingDirectories)
							sb.AppendLine(dir);
						foreach (var file in matchingFiles)
							sb.AppendLine(file);

						if (total == 0)
						{
							sb.AppendLine(LocalizationManager.LocalizeStatic("fs-glob_no_matches"));
							result.StatusIcon = Material.Icons.MaterialIconKind.FolderOpen;
							result.StatusTitle = $"**{pattern}** *{string.Format(LocalizationManager.LocalizeStatic("fs-glob_results"), 0)}*";
						}
						else
						{
							result.StatusIcon = Material.Icons.MaterialIconKind.FolderMultiple;
							result.StatusTitle = $"**{pattern}** *({string.Format(LocalizationManager.LocalizeStatic("fs-glob_results"), total)})*";
						}

						result.ResultContent = sb.ToString();
						result.Complete(true);
					}
					catch (OperationCanceledException)
					{
						result.ResultContent = "Glob search cancelled.";
						result.Complete(false);
					}
					catch (Exception ex)
					{
						result.StatusIcon = null;
						result.StatusTitle = null;
						result.ResultContent = $"Error during glob search: {ex.Message}";
						result.Complete(false);
					}
				}, cancellationToken);

				return result;
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Glob error: {ex.Message}");
			}
		}
	}
}
