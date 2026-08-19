using System.Text.Json.Nodes;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// Manages meta tools created by LLM. Supports multiple scripting engines
	/// (Lua, Python, CSX, etc.) through <see cref="IMetaToolEngine"/>.
	/// Tools are stored as files in <see cref="Utils.Directories.Metatools"/> and in working
	/// directory <c>.llmassist/metatools</c> folders; reading is delegated to
	/// <see cref="IMetaToolLocator"/> and <see cref="IMetaToolLoader"/>.
	/// </summary>
	[ChatService(typeof(IMetaToolManagementService))]
	public class MetaToolManagementService : IMetaToolManagementService
	{
		private readonly IMetaToolParser _parser;
		private readonly IMetaToolLocator _locator;
		private readonly IMetaToolLoader _loader;
		private readonly IChatSettingsService _chatSettings;
		private readonly Dictionary<string, IMetaToolEngine> _enginesByExtension;
		private readonly Dictionary<ScriptLanguageType, IMetaToolEngine> _enginesByLanguage;

		/// <summary>
		/// Initializes a new instance of the <see cref="MetaToolManagementService"/> class.
		/// </summary>
		/// <param name="parser">The parser used to serialize tool files.</param>
		/// <param name="locator">The locator used to find tool files.</param>
		/// <param name="loader">The loader used to read tool files.</param>
		/// <param name="engines">The scripting engines.</param>
		/// <param name="chatSettings">The chat settings used to resolve working directories.</param>
		public MetaToolManagementService(
			IMetaToolParser parser,
			IMetaToolLocator locator,
			IMetaToolLoader loader,
			IEnumerable<IMetaToolEngine> engines,
			IChatSettingsService chatSettings)
		{
			_parser = parser;
			_locator = locator;
			_loader = loader;
			_chatSettings = chatSettings;
			_enginesByExtension = new Dictionary<string, IMetaToolEngine>(StringComparer.OrdinalIgnoreCase);
			_enginesByLanguage = [];

			foreach (var engine in engines)
			{
				foreach (var extension in engine.Descriptor.Extensions)
					_enginesByExtension[extension] = engine;
				_enginesByLanguage[engine.Language] = engine;
			}
		}

		/// <inheritdoc/>
		public void CreateOrUpdateTool(string name, bool? isLocal,
			string? description, string? title, string? category,
			ToolApprovalLevel? approvalLevel, ToolBehaviour? behaviours, JsonObject? argumentSchema,
			ScriptLanguageType? language, string? executionCode)
		{
			ArgumentNullException.ThrowIfNull(name);

			var existingFile = FindToolFile(name);
			IMetaToolEngine engine;

			if (existingFile is not null)
			{
				// Updating existing tool — detect engine from file extension
				var extension = Path.GetExtension(existingFile.FileName);
				if (!_enginesByExtension.TryGetValue(extension, out engine!))
					throw new NotSupportedException($"No engine found for extension '{extension}'.");

				var existingTool = LoadTool(existingFile);
				if (existingTool is null)
					throw new InvalidOperationException($"Could not load the existing tool '{name}'.");

				var updatedTool = new MetaToolInfo
				{
					Name = name,
					Title = title ?? existingTool.Title,
					Description = description ?? existingTool.Description,
					Category = category ?? existingTool.Category,
					ApprovalLevel = approvalLevel ?? existingTool.ApprovalLevel,
					Behaviours = behaviours ?? existingTool.Behaviours,
					ArgumentSchema = argumentSchema ?? existingTool.ArgumentSchema,
					ScriptLanguage = language ?? existingTool.ScriptLanguage,
					ExecutionCode = executionCode ?? existingTool.ExecutionCode,
					Source = isLocal == true ? MetaToolSource.WorkingDirectory : existingTool.Source
				};

				string? fileToOverwrite = existingFile.FileName;

				// If the language changed, we need a different engine and a new file format
				if (updatedTool.ScriptLanguage != existingTool.ScriptLanguage)
				{
					if (!_enginesByLanguage.TryGetValue(updatedTool.ScriptLanguage, out var newEngine))
						throw new NotSupportedException($"No engine found for language '{updatedTool.ScriptLanguage}'.");

					// Delete old file, write new one with the new engine's format
					File.Delete(fileToOverwrite);
					fileToOverwrite = null;
					engine = newEngine;
				}

				// If the scope changed, write to the new location and delete the old file
				else if (updatedTool.Source != existingTool.Source)
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

				var metaTool = new MetaToolInfo
				{
					Name = name,
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
					ExecutionCode = executionCode,
					Source = isLocal == true ? MetaToolSource.WorkingDirectory : MetaToolSource.UserProfile
				};

				WriteToolFile(metaTool, engine.Descriptor, null);
			}
		}

		/// <inheritdoc/>
		public MetaToolInfo[] ListTools()
		{
			return _loader.Load(_locator.LocateMetaToolFiles())
				.OrderBy(t => t.Category)
				.ThenBy(t => t.Title)
				.ToArray();
		}

		/// <inheritdoc/>
		public void RenameTool(string oldName, string newName)
		{
			var oldFile = FindToolFile(oldName);
			if (oldFile is null)
				throw new KeyNotFoundException($"Could not find a tool with the name '{oldName}'");

			var extension = Path.GetExtension(oldFile.FileName);
			var newFile = oldFile.Source == MetaToolSource.UserProfile
				? Path.Combine(Directories.Metatools, newName + extension)
				: Path.Combine(Path.GetDirectoryName(oldFile.FileName)!, newName + extension);

			if (File.Exists(newFile))
				throw new InvalidOperationException($"A tool with the name '{newName}' already exists.");

			File.Move(oldFile.FileName, newFile);
		}

		/// <inheritdoc/>
		public void DeleteTool(string name)
		{
			var file = FindToolFile(name);
			if (file is null)
				throw new KeyNotFoundException($"Could not find a tool with the name '{name}'");

			File.Delete(file.FileName);
		}

		/// <inheritdoc/>
		public void SaveToolFile(string name, string content)
		{
			var file = FindToolFile(name);
			if (file is null)
				throw new KeyNotFoundException($"Could not find a tool with the name '{name}'");

			File.WriteAllText(file.FileName, content);
		}

		/// <inheritdoc/>
		public ToolInfo[] GetMetaTools()
		{
			var result = new List<ToolInfo>();

			foreach (var tool in _loader.Load(_locator.LocateMetaToolFiles()))
			{
				if (tool.Diagnostic?.IsFatal == true)
					continue;

				if (!_enginesByLanguage.TryGetValue(tool.ScriptLanguage, out var engine))
					continue;

				var desc = tool.Description;
				result.Add(new ToolInfo
				{
					Name = tool.Name,
					DescriptionGetter = () => desc,
					ArgumentSchema = tool.ArgumentSchema ?? [],
					Executor = engine.CreateExecutor(tool),
					DefaultExpectedBehaviour = ToolBehaviour.Meta | tool.Behaviours,
					TitleKey = !string.IsNullOrEmpty(tool.Title) ? Locale.GetConstKey(tool.Title) : null,
					CategoryKey = !string.IsNullOrEmpty(tool.Category) ? Locale.GetConstKey(tool.Category) : null,
					Source = ToolSource.Meta,
					ApprovalLevel = tool.ApprovalLevel,
					Enabled = true
				});
			}

			return result.ToArray();
		}

		private MetaToolInfo? LoadTool(MetaToolFileInfo file)
		{
			return _loader.Load([file]).FirstOrDefault();
		}

		private MetaToolFileInfo? FindToolFile(string name)
		{
			return _locator.LocateMetaToolFiles()
				.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f.FileName) == name);
		}

		private void WriteToolFile(MetaToolInfo tool, IMetaToolEngineDescriptor engineDescriptor, string? filePath)
		{
			var content = _parser.Serialize(tool, engineDescriptor);
			if (filePath is null)
			{
				if (tool.Source != MetaToolSource.UserProfile)
					filePath = Path.Combine(_chatSettings.Settings.Environment.GetEffectiveWorkingDirectories().GetWorkingDirectory(), Directories.WorkingHome, "metatools", tool.Name + engineDescriptor.MainExtension);
				else
					filePath = Path.Combine(Directories.Metatools, tool.Name + engineDescriptor.MainExtension);
			}
			File.WriteAllText(filePath, content);
		}
	}
}
