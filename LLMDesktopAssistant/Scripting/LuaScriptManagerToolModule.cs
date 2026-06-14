using System.ComponentModel;
using System.Text;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using Material.Icons;
using RCLargeLanguageModels.Tools;
using UglyToad.PdfPig.Graphics.Operations.PathPainting;

namespace LLMDesktopAssistant.Scripting
{
	/// <summary>
	/// Module for managing user Lua scripts — scripts that are loaded into the Lua environment
	/// at runtime and provide custom namespaced APIs.
	/// </summary>
	[ToolModule]
	public class LuaScriptManagerToolModule : ToolModule
	{
		private readonly ILuaUserScriptManager _scriptManager;

		public LuaScriptManagerToolModule(ILuaUserScriptManager scriptManager)
		{
			_scriptManager = scriptManager;

			AddTool(RegisterOrUpdateLuaScript,
				new ToolInitializationInfo
				{
					Name = "lua-register_or_update_script",
					Description = BuildRegisterOrUpdateDescription(),
					Category = "Lua",
					DefaultExpectedBehaviour = ToolBehaviour.PossiblyUnexpected
				});

			AddTool(RemoveLuaScript,
				new ToolInitializationInfo
				{
					Name = "lua-remove_script",
					Description = "Removes a registered Lua user script by its path. The path is relative to the Lua scripts directory.",
					Category = "Lua",
					DefaultExpectedBehaviour = ToolBehaviour.PossiblyUnexpected
				});

			AddTool(MoveLuaScript,
				new ToolInitializationInfo
				{
					Name = "lua-move_script",
					Description = "Moves or renames a Lua user script from one path to another. Both paths are relative to the Lua scripts directory.",
					Category = "Lua",
					DefaultExpectedBehaviour = ToolBehaviour.PossiblyUnexpected
				});

			AddTool(ListLuaScripts,
				new ToolInitializationInfo
				{
					Name = "lua-list_scripts",
					Description = "Lists all registered Lua user scripts with their namespace, path, and manuals.",
					Category = "Lua"
				});

			AddTool(GetLuaScriptInfo,
				new ToolInitializationInfo
				{
					Name = "lua-get_script_info",
					Description = "Gets detailed information about a specific Lua user script including its full content.",
					Category = "Lua"
				});
		}

		private string BuildRegisterOrUpdateDescription()
		{
			var sb = new StringBuilder();
			sb.AppendLine("Registers a new Lua user script or updates an existing one.");
			sb.AppendLine("The script will be loaded into the Lua environment instantly and will be available");
			sb.AppendLine("in the specified namespace within the Lua runtime.");
			sb.AppendLine();
			sb.AppendLine("The script file will be created in the Lua scripts directory.");
			sb.AppendLine("File extension (.lua) will be added automatically if not provided.");
			sb.AppendLine();
			sb.AppendLine("The script can use the `_NS` global variable which points to its own namespace table.");
			sb.AppendLine("Example defining a function in a custom namespace:");
			sb.AppendLine();
			sb.AppendLine("""
				```
				function _NS.greet(name)
					return "Hello, " .. name .. "!"
				end
				```
				""");
			sb.AppendLine();
			sb.AppendLine("This would create a script that adds `mytools.utils.greet()` to the Lua environment.");
			sb.AppendLine("The NAMESPACE and MANUALS metadata are automatically inserted by the system.");
			sb.AppendLine();
			sb.AppendLine("Notes:");
			sb.AppendLine("- Use a dot-separated namespace like 'mytools.utils' or 'myplugin'.");
			sb.AppendLine("- The manuals text is used by LLM as documentation for your API.");
			sb.AppendLine("- Inside the script, use `_NS` table to define your functions.");

			return sb.ToString();
		}

		public ReactiveToolResult RegisterOrUpdateLuaScript(
			[Description("The path of the script file relative to the Lua scripts directory. Example: 'mytools/utils.lua' or 'myplugin.lua'")]
			string path,
			[Description("The namespace for the script. Example: 'mytools.utils' or 'myplugin'. Use empty string for global namespace.")]
			string? ns = null,
			[Description("A documentation string that describes the script's API. This will be shown to LLM when using the manuals() function.")]
			string? manuals = null,
			[Description("The Lua script content. Use `_NS` table to define functions in the namespace.")]
			string? script = null)
		{
			try
			{
				path = LuaUserScriptManager.NormalizeScriptPath(path);

				_scriptManager.RegisterOrUpdateScript(path, ns, manuals, script);

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.LanguageLua,
					ResultContent = $"Script '{path}' (namespace: '{ns}') registered successfully."
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.LanguageLua,
					ResultContent = $"Failed to register script: {ex.Message}"
				}.CompleteWithError();
			}
		}

		public ReactiveToolResult RemoveLuaScript(
			[Description("The path of the script to remove, relative to the Lua scripts directory. Example: 'mytools/utils.lua'")]
			string path)
		{
			try
			{
				path = LuaUserScriptManager.NormalizeScriptPath(path);

				if (_scriptManager.RemoveScript(path))
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.LanguageLua,
						ResultContent = $"Script '{path}' removed successfully."
					}.CompleteWithSuccess();
				else
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.LanguageLua,
						ResultContent = $"Script '{path}' not found or could not be removed."
					}.CompleteWithError();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.LanguageLua,
					ResultContent = $"Failed to remove script: {ex.Message}"
				}.CompleteWithError();
			}
		}

		public ReactiveToolResult MoveLuaScript(
			[Description("The current path of the script, relative to the Lua scripts directory. Example: 'mytools/utils.lua'")]
			string oldPath,
			[Description("The new path for the script, relative to the Lua scripts directory. Example: 'mytools/strings.lua'")]
			string newPath)
		{
			try
			{
				oldPath = LuaUserScriptManager.NormalizeScriptPath(oldPath);
				newPath = LuaUserScriptManager.NormalizeScriptPath(newPath);

				if (_scriptManager.MoveScript(oldPath, newPath))
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.LanguageLua,
						ResultContent = $"Script moved from '{oldPath}' to '{newPath}' successfully."
					}.CompleteWithSuccess();
				else
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.LanguageLua,
						ResultContent = $"Failed to move script. Check if source exists and destination doesn't."
					}.CompleteWithError();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.LanguageLua,
					ResultContent = $"Failed to move script: {ex.Message}"
				}.CompleteWithError();
			}
		}

		public ReactiveToolResult ListLuaScripts()
		{
			try
			{
				var scripts = _scriptManager.GetScripts().ToList();

				if (scripts.Count == 0)
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.LanguageLua,
						ResultContent = $"No Lua user scripts registered."
					}.CompleteWithSuccess();

				var result = new StringBuilder();
				result.AppendLine("Registered Lua user scripts:");
				result.AppendLine();

				foreach (var script in scripts)
				{
					result.AppendLine($"- {script.Path} (namespace: {script.Namespace ?? "*global namespace*"})");
				}

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.LanguageLua,
					ResultContent = result.ToString()
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.LanguageLua,
					ResultContent = $"Failed to list scripts: {ex.Message}"
				}.CompleteWithError();
			}
		}

		public ReactiveToolResult GetLuaScriptInfo(
			[Description("The path of the script to get info for, relative to the Lua scripts directory. Example: 'mytools/utils.lua'")]
			string path)
		{
			try
			{
				path = LuaUserScriptManager.NormalizeScriptPath(path);

				var scripts = _scriptManager.GetScripts().ToList();
				var script = scripts.FirstOrDefault(s =>
					string.Equals(s.Path, path, StringComparison.OrdinalIgnoreCase));

				if (script == null)
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.LanguageLua,
						ResultContent = $"No Lua user script with path '{path}' found."
					}.CompleteWithError();

				var result = new StringBuilder();
				result.AppendLine($"Path: {script.Path}");
				result.AppendLine($"Namespace: {script.Namespace ?? "*global namespace*"}");
				result.AppendLine();
				result.AppendLine("Manuals:");
				result.AppendLine(script.Manuals ?? "(no manuals)");
				result.AppendLine();
				result.AppendLine("Script content:");
				result.AppendLine("```lua");
				result.AppendLine(script.Script);
				result.AppendLine("```");

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.LanguageLua,
					ResultContent = result.ToString()
				}.CompleteWithError();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.LanguageLua,
					ResultContent = $"Failed to get script info: {ex.Message}"
				}.CompleteWithError();
			}
		}
	}
}
