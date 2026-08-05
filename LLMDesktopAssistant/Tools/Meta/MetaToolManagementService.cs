using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using DocumentFormat.OpenXml.Wordprocessing;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// Manages meta tools created by LLM. Supports multiple scripting engines
	/// (Lua, Python, etc.) through <see cref="IMetaToolEngine"/>.
	/// Tools are stored as files in <see cref="Utils.Directories.Metatools"/>.
	/// </summary>
	[ChatService(typeof(IMetaToolManagementService))]
	public class MetaToolManagementService : IMetaToolManagementService
	{
		private class MetaToolCacheEntry
		{
			public required DateTime LastWriteTime { get; init; }
			public required MetaTool MetaTool { get; init; }
		}

		private readonly ConcurrentDictionary<string, MetaToolCacheEntry> _cache = [];

		private readonly Chat _chat;
		private readonly IMetaToolSerializer _serializer;
		private readonly Dictionary<string, IMetaToolEngine> _enginesByExtension;
		private readonly Dictionary<ScriptLanguageType, IMetaToolEngine> _enginesByLanguage;

		public MetaToolManagementService(IMetaToolSerializer serializer, IEnumerable<IMetaToolEngine> engines, Chat chat)
		{
			_chat = chat;
			_serializer = serializer;
			_enginesByExtension = new Dictionary<string, IMetaToolEngine>(StringComparer.OrdinalIgnoreCase);
			_enginesByLanguage = [];

			foreach (var engine in engines)
			{
				foreach (var extension in engine.Descriptor.Extensions)
					_enginesByExtension[extension] = engine;
				_enginesByLanguage[engine.Language] = engine;
			}
		}

		public void CreateOrUpdateTool(string name, bool? isLocal,
			string? description, string? title, string? category,
			ToolApprovalLevel? approvalLevel, ToolBehaviour? behaviours, JsonObject? argumentSchema,
			ScriptLanguageType? language, string? executionCode)
		{
			ArgumentNullException.ThrowIfNull(name);

			var existingFile = FindToolFile(name);
			IMetaToolEngine engine;

			if (existingFile.Path != null)
			{
				// Updating existing tool — detect engine from file extension
				var extension = Path.GetExtension(existingFile.Path);
				if (!_enginesByExtension.TryGetValue(extension, out engine!))
					throw new NotSupportedException($"No engine found for extension '{extension}'.");

				var existingTool = DeserializeToolFile(existingFile.Path, existingFile.IsLocal, engine.Descriptor)!;
				var updatedTool = new MetaTool
				{
					Name = name,
					IsLocal = isLocal ?? existingTool.IsLocal,
					Title = title ?? existingTool.Title,
					Description = description ?? existingTool.Description,
					Category = category ?? existingTool.Category,
					ApprovalLevel = approvalLevel ?? existingTool.ApprovalLevel,
					Behaviours = behaviours ?? existingTool.Behaviours,
					ArgumentSchema = argumentSchema ?? existingTool.ArgumentSchema,
					ScriptLanguage = language ?? existingTool.ScriptLanguage,
					ExecutionCode = executionCode ?? existingTool.ExecutionCode
				};

				string? fileToOverwrite = existingFile.Path;

				// If language changed, we might need a different engine
				if (updatedTool.ScriptLanguage != existingTool.ScriptLanguage)
				{
					if (!_enginesByLanguage.TryGetValue(updatedTool.ScriptLanguage, out var newEngine))
						throw new NotSupportedException($"No engine found for language '{updatedTool.ScriptLanguage}'.");

					// Delete old file, write new one with new engine's format
					File.Delete(fileToOverwrite);
					fileToOverwrite = null;
					engine = newEngine;
				}

				// If scope changed from local to app, we need to delete local file
				else if (!updatedTool.IsLocal && existingTool.IsLocal)
				{
					File.Delete(fileToOverwrite);
					fileToOverwrite = null;
				}

				WriteToolFile(updatedTool, engine.Descriptor, fileToOverwrite);
			}
			else
			{
				// Creating new tool
				if (language == null) throw new ArgumentNullException(nameof(language));
				if (executionCode == null) throw new ArgumentNullException(nameof(executionCode));

				if (!_enginesByLanguage.TryGetValue(language.Value, out engine!))
					throw new NotSupportedException($"No engine found for language '{language.Value}'.");

				var metaTool = new MetaTool
				{
					Name = name,
					IsLocal = isLocal ?? false,
					Title = title ?? name,
					Description = description ?? $"Meta tool '{name}'.",
					Category = category ?? "general",
					ApprovalLevel = approvalLevel ?? ToolApprovalLevel.AlwaysAsk,
					Behaviours = behaviours ?? ToolBehaviour.None,
					ArgumentSchema = argumentSchema ?? new JsonObject
					{
						["type"] = "object",
						["properties"] = new JsonObject(),
						["additionalProperties"] = false
					},
					ScriptLanguage = language.Value,
					ExecutionCode = executionCode
				};

				WriteToolFile(metaTool, engine.Descriptor, null);
			}
		}

		private List<(string Path, bool IsLocal)> GetMetaToolFiles()
		{
			var files = Directory.Exists(Directories.Metatools) ? Directory.GetFiles(Directories.Metatools).Select(f => (f, false)).ToList() : [];
			if (_chat.Settings.Tools.FetchFromAllWorkingDirectories)
			{
				foreach (var workdir in _chat.Settings.Environment.GetEffectiveWorkingDirectories().GetEnabledWorkingDirectories())
				{
					var searchDir = Path.Combine(workdir, Directories.WorkingHome, "metatools");
					if (Directory.Exists(searchDir))
						files.AddRange(Directory.GetFiles(searchDir).Select(f => (f, true)));
				}
			}
			else
			{
				var workdir = _chat.Settings.Environment.GetEffectiveWorkingDirectories().GetWorkingDirectory();
				var searchDir = Path.Combine(workdir, Directories.WorkingHome, "metatools");
				if (Directory.Exists(searchDir))
					files.AddRange(Directory.GetFiles(searchDir).Select(f => (f, true)));
			}
			return files;
		}

		public MetaTool[] ListTools()
		{
			return GetMetaToolFiles()
				.Select(f => DeserializeToolFile(f.Path, f.IsLocal))
				.Where(t => t != null)
				.OrderBy(t => t!.Category)
				.ThenBy(t => t!.Title)
				.ToArray()!;
		}

		public void RenameTool(string oldName, string newName)
		{
			var oldFile = FindToolFile(oldName);
			if (oldFile.Path is null)
				throw new KeyNotFoundException($"Could not find a tool with the name '{oldName}'");

			var extension = Path.GetExtension(oldFile.Path);
			var newFile = oldFile.IsLocal
				? Path.Combine(_chat.Settings.Environment.GetEffectiveWorkingDirectories().GetWorkingDirectory(), Directories.WorkingHome, "metatools", newName + extension)
				: Path.Combine(Directories.Metatools, newName + extension);

			if (File.Exists(newFile))
				throw new InvalidOperationException($"A tool with the name '{newName}' already exists.");

			File.Move(oldFile.Path, newFile);
		}

		public void DeleteTool(string name)
		{
			var file = FindToolFile(name);
			if (file.Path is null)
				throw new KeyNotFoundException($"Could not find a tool with the name '{name}'");

			File.Delete(file.Path);
		}

		public ToolInfo[] GetMetaTools()
		{
			var result = new List<ToolInfo>();

			foreach (var (file, isLocal) in GetMetaToolFiles())
			{
				var ext = Path.GetExtension(file);
				if (!_enginesByExtension.TryGetValue(ext, out var engine))
					continue;
				
				try
				{
					var tool = DeserializeToolFile(file, isLocal, engine.Descriptor);
					if (tool == null) continue;

					var desc = tool.Description;
					result.Add(new ToolInfo
					{
						Name = tool.Name,
						DescriptionGetter = () => desc,
						ArgumentSchema = tool.ArgumentSchema ?? [],
						Executor = engine.CreateExecutor(tool),
						DefaultExpectedBehaviour = ToolBehaviour.Meta | tool.Behaviours,
						DisplayName = tool.Title,
						Category = tool.Category,
						Source = ToolSource.Meta,
						ApprovalLevel = tool.ApprovalLevel,
						Enabled = true
					});
				}
				catch (Exception ex)
				{
					// Log and skip invalid tool files
					System.Diagnostics.Debug.WriteLine($"Failed to load meta tool '{file}': {ex.Message}");
				}
			}

			return result.ToArray();
		}

		private MetaTool? DeserializeToolFile(string filePath, bool isLocal)
		{
			var ext = Path.GetExtension(filePath);
			if (!_enginesByExtension.TryGetValue(ext, out var engine))
				return null;
			return DeserializeToolFile(filePath, isLocal, engine.Descriptor);
		}

		private MetaTool DeserializeToolFile(string filePath, bool isLocal, IMetaToolEngineDescriptor engineDescriptor)
		{
			var fileInfo = new FileInfo(filePath);

			MetaToolCacheEntry CreateCacheEntry()
			{
				var content = File.ReadAllText(filePath);
				var name = Path.GetFileNameWithoutExtension(filePath);
				return new MetaToolCacheEntry
				{
					MetaTool = _serializer.Deserialize(content, name, isLocal, engineDescriptor),
					LastWriteTime = fileInfo.LastWriteTime
				};
			}

			return _cache.AddOrUpdate(filePath, filePath =>
			{
				return CreateCacheEntry();
			}, (filePath, entry) =>
			{
				if (fileInfo.LastWriteTime == entry.LastWriteTime)
					return entry;
				return CreateCacheEntry();
			}).MetaTool;
		}

		private void WriteToolFile(MetaTool tool, IMetaToolEngineDescriptor engineDescriptor, string? filePath)
		{
			var content = _serializer.Serialize(tool, engineDescriptor);
			if (filePath is null)
			{
				if (tool.IsLocal)
					filePath = Path.Combine(_chat.Settings.Environment.GetEffectiveWorkingDirectories().GetWorkingDirectory(), Directories.WorkingHome, "metatools", tool.Name + engineDescriptor.MainExtension);
				else
					filePath = Path.Combine(Directories.Metatools, tool.Name + engineDescriptor.MainExtension);
			}
			File.WriteAllText(filePath, content);
			var fileInfo = new FileInfo(filePath);
			_cache[filePath] = new MetaToolCacheEntry
			{
				MetaTool = tool,
				LastWriteTime = fileInfo.LastWriteTime
			};
		}

		private (string? Path, bool IsLocal) FindToolFile(string name)
		{
			return GetMetaToolFiles()
				.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f.Path) == name);
		}
	}
}
