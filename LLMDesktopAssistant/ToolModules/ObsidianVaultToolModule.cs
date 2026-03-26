using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using LLMDesktopAssistant.Modules;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.ToolModules
{
	// [Module]
	public class ObsidianVaultToolModule : ToolModule
	{
		private readonly string _vaultPath;

		public ObsidianVaultToolModule()
		{
			var vaultPath = Environment.GetEnvironmentVariable("OBSIDIAN_VAULT_PATH");
			if (string.IsNullOrEmpty(vaultPath))
			{
				_vaultPath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					"obsidian",
					"vaults"
				);
			}
			else
			{
				_vaultPath = vaultPath;
			}

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ListNotes, "obsidian-list_notes", "List all markdown notes in the Obsidian vault.")
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ReadNote, "obsidian-read_note", "Read content of a specific note by filename (without .md extension).")
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(CreateNote, "obsidian-create_note", "Create a new note in the Obsidian vault.")
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(UpdateNote, "obsidian-update_note", "Update an existing note's content.")
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(SearchNotes, "obsidian-search_notes", "Search notes by query in content or filename.")
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(GetNoteMetadata, "obsidian-get_note_metadata", "Get metadata (frontmatter) of a note.")
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(DeleteNote, "obsidian-delete_note", "Delete a note from the vault.")
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ListVaults, "obsidian-list_vaults", "List all available Obsidian vaults.")
			});
		}

		private string GetVaultFullPath(string? vaultName = null)
		{
			if (string.IsNullOrEmpty(vaultName))
			{
				var dirs = Directory.GetDirectories(_vaultPath);
				if (dirs.Length == 0)
					throw new InvalidOperationException("No Obsidian vaults found. Set OBSIDIAN_VAULT_PATH environment variable.");
				return dirs[0];
			}
			return Path.Combine(_vaultPath, vaultName);
		}

		private ToolResult ListVaults(
			[Description("Show full path for each vault")] bool showPath = false)
		{
			if (!Directory.Exists(_vaultPath))
				return new ToolResult(ToolResultStatus.Error, $"Vault path does not exist: {_vaultPath}");

			var vaults = Directory.GetDirectories(_vaultPath);
			if (vaults.Length == 0)
				return new ToolResult("No vaults found.");

			var sb = new System.Text.StringBuilder();
			foreach (var vault in vaults)
			{
				var name = Path.GetFileName(vault);
				sb.AppendLine(showPath ? $"{name}: {vault}" : name);
			}
			return new ToolResult(sb.ToString().Trim());
		}

		private ToolResult ListNotes(
			[Description("Vault name (optional, uses first vault if not specified)")] string? vaultName = null,
			[Description("Maximum number of notes to return")] int maxNotes = 100)
		{
			try
			{
				var vaultPath = GetVaultFullPath(vaultName);
				var notes = Directory.GetFiles(vaultPath, "**/*.md", SearchOption.AllDirectories)
					.Select(f => Path.GetRelativePath(vaultPath, f))
					.Take(maxNotes)
					.ToList();

				if (notes.Count == 0)
					return new ToolResult("No notes found in vault.");

				return new ToolResult($"Found {notes.Count} notes:\n" + string.Join("\n", notes));
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error listing notes: {ex.Message}");
			}
		}

		private ToolResult ReadNote(
			[Description("Note filename without .md extension")] string filename,
			[Description("Vault name (optional)")] string? vaultName = null,
			[Description("Include frontmatter in output")] bool includeFrontmatter = true)
		{
			try
			{
				var vaultPath = GetVaultFullPath(vaultName);
				var filePath = FindNoteFile(vaultPath, filename);

				if (filePath == null)
					return new ToolResult(ToolResultStatus.Error, $"Note '{filename}' not found.");

				var content = File.ReadAllText(filePath);
				if (!includeFrontmatter)
				{
					content = RemoveFrontmatter(content);
				}

				return new ToolResult($"# {filename}\n\n{content}");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error reading note: {ex.Message}");
			}
		}

		private ToolResult CreateNote(
			[Description("Note filename without .md extension")] string filename,
			[Description("Note content (markdown)")] string content,
			[Description("Vault name (optional)")] string? vaultName = null,
			[Description("Frontmatter as JSON object")] string? frontmatter = null)
		{
			try
			{
				var vaultPath = GetVaultFullPath(vaultName);
				var filePath = Path.Combine(vaultPath, filename + ".md");

				if (File.Exists(filePath))
					return new ToolResult(ToolResultStatus.Error, $"Note '{filename}' already exists.");

				var fullContent = content;
				if (!string.IsNullOrEmpty(frontmatter))
				{
					var fm = "---\n" + frontmatter + "\n---\n";
					fm = fm.Replace("{", "").Replace("}", "");
					fm = Regex.Replace(fm, "\"(//w+)\":", "$1:");
					fm = fm.Replace(":", ": ").Replace("  ", " ");
					fullContent = fm + content;
				}

				File.WriteAllText(filePath, fullContent);
				return new ToolResult($"Note '{filename}' created successfully at: {filePath}");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error creating note: {ex.Message}");
			}
		}

		private ToolResult UpdateNote(
			[Description("Note filename without .md extension")] string filename,
			[Description("New content to append or prepend")] string content,
			[Description("Vault name (optional)")] string? vaultName = null,
			[Description("Append to end or prepend to beginning")] bool append = true)
		{
			try
			{
				var vaultPath = GetVaultFullPath(vaultName);
				var filePath = FindNoteFile(vaultPath, filename);

				if (filePath == null)
					return new ToolResult(ToolResultStatus.Error, $"Note '{filename}' not found.");

				var existing = File.ReadAllText(filePath);
				existing = RemoveFrontmatter(existing);

				var newContent = append
					? existing.TrimEnd() + "\n\n" + content
					: content + "\n\n" + existing.TrimStart();

				File.WriteAllText(filePath, newContent);
				return new ToolResult($"Note '{filename}' updated successfully.");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error updating note: {ex.Message}");
			}
		}

		private ToolResult SearchNotes(
			[Description("Search query")] string query,
			[Description("Vault name (optional)")] string? vaultName = null,
			[Description("Search in filenames only")] bool filenameOnly = false,
			[Description("Maximum results")] int maxResults = 20)
		{
			try
			{
				var vaultPath = GetVaultFullPath(vaultName);
				var notes = Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories);
				var results = new List<string>();

				foreach (var note in notes)
				{
					var relativePath = Path.GetRelativePath(vaultPath, note);
					if (relativePath.Contains(query, StringComparison.OrdinalIgnoreCase))
					{
						results.Add(relativePath);
						continue;
					}

					if (!filenameOnly)
					{
						var content = File.ReadAllText(note);
						if (content.Contains(query, StringComparison.OrdinalIgnoreCase))
						{
							results.Add(relativePath);
						}
					}

					if (results.Count >= maxResults)
						break;
				}

				if (results.Count == 0)
					return new ToolResult($"No notes found matching '{query}'.");

				return new ToolResult($"Found {results.Count} notes:\n" + string.Join("\n", results));
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error searching notes: {ex.Message}");
			}
		}

		private ToolResult GetNoteMetadata(
			[Description("Note filename without .md extension")] string filename,
			[Description("Vault name (optional)")] string? vaultName = null)
		{
			try
			{
				var vaultPath = GetVaultFullPath(vaultName);
				var filePath = FindNoteFile(vaultPath, filename);

				if (filePath == null)
					return new ToolResult(ToolResultStatus.Error, $"Note '{filename}' not found.");

				var content = File.ReadAllText(filePath);
				var frontmatter = ExtractFrontmatter(content);

				if (frontmatter == null)
					return new ToolResult($"Note '{filename}' has no frontmatter.");

				return new ToolResult($"Frontmatter for '{filename}':\n```yaml\n{frontmatter}\n```");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error getting metadata: {ex.Message}");
			}
		}

		private ToolResult DeleteNote(
			[Description("Note filename without .md extension")] string filename,
			[Description("Vault name (optional)")] string? vaultName = null)
		{
			try
			{
				var vaultPath = GetVaultFullPath(vaultName);
				var filePath = FindNoteFile(vaultPath, filename);

				if (filePath == null)
					return new ToolResult(ToolResultStatus.Error, $"Note '{filename}' not found.");

				File.Delete(filePath);
				return new ToolResult($"Note '{filename}' deleted successfully.");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error deleting note: {ex.Message}");
			}
		}

		private string? FindNoteFile(string vaultPath, string filename)
		{
			var direct = Path.Combine(vaultPath, filename + ".md");
			if (File.Exists(direct))
				return direct;

			var files = Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories);
			return files.FirstOrDefault(f =>
				Path.GetFileNameWithoutExtension(f).Equals(filename, StringComparison.OrdinalIgnoreCase));
		}

		private string RemoveFrontmatter(string content)
		{
			if (!content.StartsWith("---"))
				return content;

			var idx = content.IndexOf("---", 3);
			if (idx < 0)
				return content;

			return content[(idx + 3)..].TrimStart();
		}

		private string? ExtractFrontmatter(string content)
		{
			if (!content.StartsWith("---"))
				return null;

			var idx = content.IndexOf("---", 3);
			if (idx < 0)
				return null;

			return content[3..idx].Trim();
		}
	}
}