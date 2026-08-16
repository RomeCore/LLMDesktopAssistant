using System.ComponentModel;
using System.Text;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Files;
using Material.Icons;
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
		private readonly WorkingDirectoryAccessService _fileAccess;

		public FilesystemGlobToolModule(WorkingDirectoryAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(new ToolInitializationInfo
			{
				Executor = Glob,
				StreamingAnalyzer = GlobStreaming,
				PreviewExecutor = GlobPreview,
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
				TitleKey = Locale.GetKey("tool.name.fs-glob"),
				DescriptionKey = Locale.GetKey("tool.description.fs-glob"),
				CategoryKey = Locale.GetKey("tool.category.filesystem"),
				DefaultExpectedBehaviour = ToolBehaviour.DirectoryRead | ToolBehaviour.AccessOutsideWorkdir
			});
		}

		private StreamingToolArgumentsAnalysisResult GlobStreaming(
			string? path, string? pattern)
		{
			path ??= "?";
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.FileSearch,
				StatusTitle = pattern != null ? $"**{path}** → **{pattern.MarkdownEscape()}**" : $"**{path}**"
			};
		}

		private PreviewToolExecutionResult GlobPreview(
			string path, string pattern, [SharedContext] out string fullPath)
		{
			fullPath = _fileAccess.CheckedAccessPath(path, DirectoryAccessMode.Read, out var isAccessed);

			if (!Directory.Exists(fullPath))
			{
				new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FolderSearch,
					StatusTitle = $"**{path}** → **{pattern.MarkdownEscape()}**",
					ExpectedBehaviour = !isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = $"Directory not found: {path}"
				};
			}

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.FolderSearch,
				StatusTitle = $"**{path}** → **{pattern.MarkdownEscape()}**",
				ExpectedBehaviour = ToolBehaviour.DirectoryRead |
					(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
			};
		}

		private ReactiveToolResult Glob(
			[SharedContext] string? fullPath,
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
				fullPath ??= _fileAccess.AccessPath(path, DirectoryAccessMode.Read);
				var workingDirectory = _fileAccess.GetWorkingDirectory();

				var result = new ReactiveToolResult();

				Task.Run(() =>
				{
					try
					{
						result.StatusIcon = MaterialIconKind.FolderSearch;
						result.StatusTitle = $"**{path}** → **{pattern.MarkdownEscape()}**";

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

									result.StatusTitle = $"**{path}** → **{pattern.MarkdownEscape()}** {LocalizationManager.LocalizeStaticFormat("tool.status.fs-glob.found_count", matchingFiles.Count + matchingDirectories.Count)}";

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

									result.StatusTitle = $"**{path}** → **{pattern.MarkdownEscape()}** {LocalizationManager.LocalizeStaticFormat("tool.status.fs-glob.found_count", matchingFiles.Count + matchingDirectories.Count)}";

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
							sb.AppendLine("No matches found.");
							result.StatusIcon = MaterialIconKind.FolderOpen;
							result.StatusTitle = $"**{path}** → **{pattern.MarkdownEscape()}** {LocalizationManager.LocalizeStaticFormat("tool.status.fs-glob.results", total)}";
						}
						else
						{
							result.StatusIcon = MaterialIconKind.FolderMultiple;
							result.StatusTitle = $"**{path}** → **{pattern.MarkdownEscape()}** {LocalizationManager.LocalizeStaticFormat("tool.status.fs-glob.results", total)}";
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
