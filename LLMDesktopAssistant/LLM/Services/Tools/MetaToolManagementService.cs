using System.Text.Json;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	/// <summary>
	/// Manages meta tools created by LLM. Supports multiple scripting engines
	/// (Lua, Python, etc.) through <see cref="IMetaToolEngine"/>.
	/// Tools are stored as files in <see cref="Utils.Directories.Metatools"/>.
	/// </summary>
	[ChatService(typeof(IMetaToolManagementService))]
	public class MetaToolManagementService : IMetaToolManagementService
	{
		private readonly Dictionary<string, IMetaToolEngine> _enginesByExtension;
		private readonly Dictionary<ScriptLanguageType, IMetaToolEngine> _enginesByLanguage;
		private readonly IServiceProvider _services;

		public MetaToolManagementService(IEnumerable<IMetaToolEngine> engines, IServiceProvider services)
		{
			_enginesByExtension = new Dictionary<string, IMetaToolEngine>(StringComparer.OrdinalIgnoreCase);
			_enginesByLanguage = new Dictionary<ScriptLanguageType, IMetaToolEngine>();

			foreach (var engine in engines)
			{
				_enginesByExtension[engine.FileExtension] = engine;
				_enginesByLanguage[engine.Language] = engine;
			}

			_services = services;
		}

		public void CreateOrUpdateTool(string name,
			string? description, string? title, string? category,
			bool? askForConfirmation, JsonObject? argumentSchema,
			ScriptLanguageType? language, string? executionCode)
		{
			ArgumentNullException.ThrowIfNull(name);

			var existingFile = FindToolFile(name);
			IMetaToolEngine engine;

			if (existingFile != null)
			{
				// Updating existing tool — detect engine from file extension
				var ext = Path.GetExtension(existingFile);
				if (!_enginesByExtension.TryGetValue(ext, out engine!))
					throw new NotSupportedException($"No engine found for file extension '{ext}'.");

				var existingTool = DeserializeToolFile(existingFile)!;
				var updatedTool = new MetaTool
				{
					Name = name,
					Title = title ?? existingTool.Title,
					Description = description ?? existingTool.Description,
					Category = category ?? existingTool.Category,
					AskForConfirmation = askForConfirmation ?? existingTool.AskForConfirmation,
					ArgumentSchema = argumentSchema ?? existingTool.ArgumentSchema,
					ScriptLanguage = language ?? existingTool.ScriptLanguage,
					ExecutionCode = executionCode ?? existingTool.ExecutionCode
				};

				// If language changed, we might need a different engine
				if (language.HasValue && language.Value != existingTool.ScriptLanguage)
				{
					if (!_enginesByLanguage.TryGetValue(language.Value, out var newEngine))
						throw new NotSupportedException($"No engine found for language '{language.Value}'.");

					// Delete old file, write new one with new engine's format
					File.Delete(existingFile);
					engine = newEngine;
				}

				WriteToolFile(updatedTool, engine);
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
					Title = title ?? name,
					Description = description ?? $"Meta tool '{name}'.",
					Category = category ?? "general",
					AskForConfirmation = askForConfirmation ?? false,
					ArgumentSchema = argumentSchema ?? new JsonObject
					{
						["type"] = "object",
						["properties"] = new JsonObject(),
						["additionalProperties"] = false
					},
					ScriptLanguage = language.Value,
					ExecutionCode = executionCode
				};

				WriteToolFile(metaTool, engine);
			}
		}

		public MetaTool[] ListTools()
		{
			if (!Directory.Exists(Utils.Directories.Metatools))
				return [];

			var files = Directory.GetFiles(Utils.Directories.Metatools);
			return files
				.Select(f => DeserializeToolFile(f))
				.Where(t => t != null)
				.OrderBy(t => t!.Category)
				.ThenBy(t => t!.Title)
				.Cast<MetaTool>()
				.ToArray();
		}

		public void RenameTool(string oldName, string newName)
		{
			var oldFile = FindToolFile(oldName)
				?? throw new KeyNotFoundException($"Could not find a tool with the name '{oldName}'");

			var extension = Path.GetExtension(oldFile);
			var newFile = Path.Combine(Utils.Directories.Metatools, newName + extension);

			if (File.Exists(newFile))
				throw new InvalidOperationException($"A tool with the name '{newName}' already exists.");

			File.Move(oldFile, newFile);
		}

		public void DeleteTool(string name)
		{
			var file = FindToolFile(name)
				?? throw new KeyNotFoundException($"Could not find a tool with the name '{name}'");

			File.Delete(file);
		}

		public ToolInfo[] GetMetaTools()
		{
			if (!Directory.Exists(Utils.Directories.Metatools))
				return [];

			var result = new List<ToolInfo>();

			foreach (var file in Directory.GetFiles(Utils.Directories.Metatools))
			{
				var ext = Path.GetExtension(file);
				if (!_enginesByExtension.TryGetValue(ext, out var engine))
					continue;

				try
				{
					var tool = DeserializeToolFile(file);
					if (tool == null) continue;

					var desc = tool.Description;
					result.Add(new ToolInfo
					{
						Name = tool.Name,
						DescriptionGetter = () => desc,
						ArgumentSchema = tool.ArgumentSchema ?? new JsonObject(),
						Executor = engine.CreateExecutor(tool),
						DefaultExpectedBehaviour = ToolBehaviour.Meta,
						DisplayName = tool.Title,
						Category = tool.Category,
						Source = ToolSource.Meta,
						ApprovalLevel = tool.AskForConfirmation ? ToolApprovalLevel.AlwaysAsk : ToolApprovalLevel.PolicyBased,
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

		private MetaTool? DeserializeToolFile(string filePath)
		{
			var ext = Path.GetExtension(filePath);
			if (!_enginesByExtension.TryGetValue(ext, out var engine))
				return null;

			var content = File.ReadAllText(filePath);
			var name = Path.GetFileNameWithoutExtension(filePath);
			return engine.Deserialize(content, name);
		}

		private void WriteToolFile(MetaTool tool, IMetaToolEngine engine)
		{
			var content = engine.Serialize(tool);
			var filePath = Path.Combine(Utils.Directories.Metatools, tool.Name + engine.FileExtension);
			File.WriteAllText(filePath, content);
		}

		private string? FindToolFile(string name)
		{
			if (!Directory.Exists(Utils.Directories.Metatools))
				return null;

			return Directory.GetFiles(Utils.Directories.Metatools)
				.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == name);
		}
	}
}
