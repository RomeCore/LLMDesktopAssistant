using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Text;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Files;
using Material.Icons;
using ModelContextProtocol.Protocol;
using Serilog;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	[ToolModule]
	public class FilesystemExploreToolModule : ToolModule
	{
		private readonly FileAccessService _fileAccess;

		public FilesystemExploreToolModule(FileAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(Explore, ExploreStreaming, ExplorePreview,
				new ToolInitializationInfo
				{
					Name = "fs-read_entry",
					Aliases = ["fs-read_file", "read_file", "fs-read_directory", "read_directory"],
					Description = """
						The universal tool for exploring the filesystem. It can list directories and read files line by line.
						If directory exists under the specified path, it will list all files and directories in that directory.
						If file exists, it will read the content of the file by lines (automatically selects 1-200 lines).
						If both exists, it will do both actions.
						""",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.FileRead | ToolBehaviour.DirectoryRead
				});
		}

		public StreamingToolArgumentsAnalysisResult ExploreStreaming(
			string? path)
		{
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.File,
				StatusTitle = $"**{path}**"
			};
		}

		public PreviewToolExecutionResult? ExplorePreview(string path,
			int startLine = 0, int endLine = 0, int maxLineLength = 2000,
			[Inject] DetectSecretsSharp.Core.Scanner scanner = default!)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var entryName = Path.GetFileName(fullPath);

				bool fileExists = File.Exists(fullPath);
				bool directoryExists = Directory.Exists(fullPath);

				if (fileExists)
				{
					if (startLine < 1)
						startLine = 1;
					if (endLine < startLine)
						endLine = startLine + 199;

					var (lines, totalLines) = FileUtils.ReadLinesChunk(
						fullPath,
						startLine,
						endLine - startLine + 1,
						maxLineLength,
						withLineNumbers: false);

					var secrets = scanner.ScanLines(lines, filename: entryName, verify: false);

					/*
					var sw = new Stopwatch();
					sw.Start();
					var secrets = scanner.ScanLines(lines, filename: displayEntryName, verify: false);
					var elapsedTime = sw.Elapsed.TotalMilliseconds;
					var timePerLine = elapsedTime / lines.Count;
					Log.Information("Scanned {Lines} lines in {TotalTime:0.00} ms, time per line: {TimePerLine:0.00} ms, detectors count: {DetectorCount}", lines.Count, elapsedTime, timePerLine, scanner.Detectors.Count);
					*/

					if (secrets.HasSecrets)
					{
						return new PreviewToolExecutionResult
						{
							StatusIcon = directoryExists ? MaterialIconKind.FileEye : MaterialIconKind.FileCode,
							StatusTitle = LocalizationManager.LocalizeStaticFormat("file_contains_secrets", $"**{path}**"),
							ExpectedBehaviour = ToolBehaviour.ReadSecrets | ToolBehaviour.FileRead |
								(directoryExists ? ToolBehaviour.DirectoryRead : 0)
						};
					}

					return new PreviewToolExecutionResult
					{
						StatusIcon = directoryExists ? MaterialIconKind.FileEye : MaterialIconKind.FileCode,
						StatusTitle = $"**{path}**",
						ExpectedBehaviour = ToolBehaviour.FileRead | (directoryExists ? ToolBehaviour.DirectoryRead : 0)
					};
				}

				if (directoryExists)
				{
					return new PreviewToolExecutionResult
					{
						StatusIcon = MaterialIconKind.Folder,
						StatusTitle = $"**{path}**",
						ExpectedBehaviour = ToolBehaviour.DirectoryRead
					};
				}

				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileDiscard,
					StatusTitle = $"**{path}**",
					InterruptingContent = $"No such file or directory found: '{path}'.",
					InterruptingSuccess = false,
					ExpectedBehaviour = ToolBehaviour.None // Will do nothing
				};
			}
			catch (Exception ex)
			{
				return new PreviewToolExecutionResult
				{
					InterruptingContent = $"Error reading file: {ex.Message}",
					InterruptingSuccess = false
				};
			}
		}

		public ReactiveToolResult Explore(
			[Description("The path of the entry to read. Leave empty to read the current working directory.")]
			string path,
			[Description("The 1-based index of the first line to read. Only for files.")]
			int startLine = 0,
			[Description("The 1-based index of the last line to read. Only for files.")]
			int endLine = 0,
			[Description("The maximum length of each line to read. Only for files.")]
			int maxLineLength = 2000,
			[Description("Whether to include line numbers before every line in format '   1: *line content*'. Only for files.")]
			bool showLineNumbers = false,
			[Description("The maximum depth of directories to include in the listing. 1 = current directory only. Only for directories.")]
			int maxDepth = 2,
			[Description("The number of entries to skip before starting the list. Only for directories.")]
			int offset = 0,
			[Description("The maximum number of filesystem entries to return. Only for directories.")]
			int maxEntries = 500,
			[Description("The list of directories to ignore. Each directory can be a pattern (e.g. '.*' to ignore all directories that starts with dot). Only for directories.")]
			[DefaultValue(new string[]{ ".git", Directories.WorkingHome })]
			string[]? ignoreDirectories = null)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var entryName = Path.GetFileName(fullPath);

				bool fileExists = File.Exists(fullPath);
				bool directoryExists = Directory.Exists(fullPath);
				bool anyExists = fileExists || directoryExists;

				var result = new ReactiveToolResult();

				var sb = new StringBuilder();
				if (fileExists)
				{
					if (startLine < 1)
						startLine = 1;
					if (endLine < startLine)
						endLine = startLine + 199;

					var (lines, totalLines) = FileUtils.ReadLinesChunk(
						fullPath,
						startLine,
						endLine - startLine + 1,
						maxLineLength,
						showLineNumbers);
					var endShown = startLine + lines.Count - 1;

					if (directoryExists)
					{
						result.StatusIcon = Material.Icons.MaterialIconKind.FileEye;
						result.StatusTitle = $"**{path}**";
					}
					else
					{
						result.StatusIcon = Material.Icons.MaterialIconKind.FileCode;
						result.StatusTitle = endShown == totalLines ?
							(startLine == 1 ? $"**{path}**" : $"**{path}** *({startLine}~{endShown})*") :
							$"**{path}** *({startLine}~{endShown} / {totalLines})*";
					}

					if (lines.Count == 0)
					{
						lines.Add("No content.");
					}

					string nextReadTip = string.Empty;
					if (endShown < totalLines)
						nextReadTip = $" (Continue reading from line {endShown + 1}, e.g. {endShown + 1}-{Math.Min(endShown + 100, totalLines)})";

					var output = $"""
					[FILE READING]
					File: {path}
					Showing: {startLine}-{endShown}{nextReadTip}
					Total lines: {totalLines}
					[CONTENT START]
					{string.Join(Environment.NewLine, lines)}
					[CONTENT END]
					""";
					sb.AppendLine(output);
				}
				if (directoryExists)
				{
					if (maxDepth < 1)
						maxDepth = 1;

					int totalCount = 0;
					ignoreDirectories ??= [".git", Directories.WorkingHome];

					void Traverse(string currentPath, int depth, string indent)
					{
						if (depth > maxDepth)
							return;

						var entries = Directory.GetFileSystemEntries(currentPath)
							.OrderBy(e => Directory.Exists(e) ? 0 : 1)
							.ThenBy(e => e);

						foreach (var entry in entries)
						{
							var name = Path.GetFileName(entry);

							if (Directory.Exists(entry))
							{
								string count = "?";
								try
								{
									count = Directory.GetFileSystemEntries(entry).Length.ToString();
								}
								catch { /* ignore */ }

								if (offset <= totalCount && totalCount <= offset + maxEntries)
								{
									try
									{
										var edited = Directory.GetLastWriteTime(entry).ToString("yyyy-MM-dd HH:mm");
										sb.AppendLine($"{indent}[DIR]  {name} ({count} items, {edited})");
									}
									catch
									{
										sb.AppendLine($"{indent}[DIR]  {name} ({count} items) [ERROR GETTING INFO]");
									}
								}

								totalCount++;

								if (ignoreDirectories.Any(igdir => FileSystemName.MatchesSimpleExpression(igdir, name)))
									sb.AppendLine($"{indent}  Directory ignored");
								else
									Traverse(entry, depth + 1, indent + "  ");
							}
							if (File.Exists(entry))
							{
								if (offset <= totalCount && totalCount <= offset + maxEntries)
								{
									try
									{
										var fileMetrics = FileUtils.GetFileMetrics(entry);
										var sizeStr = FileUtils.BytesToDisplaySize(fileMetrics.Size);
										var linesStr = fileMetrics.LineCount != null ? $"{fileMetrics.LineCount} lines" : "binary";
										var edited = fileMetrics.Modified.ToString("yyyy-MM-dd HH:mm");

										sb.AppendLine($"{indent}[FILE] {name} ({sizeStr}, {linesStr}, {edited})");
									}
									catch
									{
										sb.AppendLine($"{indent}[FILE] {name} [ERROR GETTING INFO]");
									}
								}

								totalCount++;
							}
						}
					}

					sb.AppendLine($"[DIRECTORY LISTING]");
					sb.AppendLine($"Path: {path}");
					sb.AppendLine($"Max depth: {maxDepth}");
					sb.AppendLine($"Offset: {offset}, Max entries: {maxEntries}");
					if (ignoreDirectories.Length > 0)
						sb.AppendLine($"Ignored directories: {string.Join(", ", ignoreDirectories)}");
					else
						sb.AppendLine("No ignored directories.");
					sb.AppendLine();

					Traverse(fullPath, 1, "");

					if (offset > totalCount)
					{
						sb.AppendLine($"Offset is too large. (offset:{offset} > total count:{totalCount})");
					}
					else if (offset + maxEntries < totalCount)
					{
						sb.AppendLine();
						sb.AppendLine($"... truncated ({totalCount - offset - maxEntries} entries more avalable)");
					}

					if (!fileExists)
					{
						result.StatusIcon = MaterialIconKind.Folder;

						int startEntry = offset;
						int totalEntries = totalCount;
						int endEntry = Math.Min(offset + maxEntries, totalEntries);
						string boldDirName = string.IsNullOrWhiteSpace(path) ? "" : $"**{path}**";
						result.StatusTitle = endEntry == totalEntries ?
							(startEntry == 0 ? boldDirName : $"{boldDirName} *({startEntry}~{endEntry})*") :
							$"{boldDirName} *({startEntry}~{endEntry} / {totalEntries})*";
					}
				}
				if (!anyExists)
				{
					result.ResultContent = $"No such file or directory found: '{path}'.";
					result.StatusIcon = MaterialIconKind.FileDiscard;
					result.StatusTitle = $"**{path}**";
					return result.Complete(false);
				}

				result.ResultContent = sb.ToString();
				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error reading file: {ex.Message}");
			}
		}
	}
}
