using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Scripting;
using Material.Icons;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule]
	public class LuaInterpreterToolModule : ToolModule
	{
		private readonly LuaService _lua;

		public LuaInterpreterToolModule(LuaService lua)
		{
			_lua = lua;

			AddTool(Execute, ExecuteStreaming, ExecutePreview,
				new ToolInitializationInfo
				{
					Name = "lua-execute",
					DescriptionGetter = () => $"""
						Executes Lua and returns the script result along with messages printed by 'print' function.
						
						Lua has the API to interact with the application (called dASS) with these namespaces:
						{string.Join(", ", lua.Namespaces.Select(ns => ns != null ? $"**{ns}**" : "*global namespace*").Order())}
						
						Use the `manuals(...)` function to get the documentation for a specific namespace, `print(manuals())`, `print(manuals(dass.tools))` or `print(manuals(fs))` for example.
						Its very recommended to see manuals before starting to use the API (do not use it blindly without reading the documentation!).
						""",
					Category = "Lua",
					DefaultExpectedBehaviour = ToolBehaviour.PossiblyUnexpected
				});
		}

		public StreamingToolArgumentsAnalysisResult ExecuteStreaming(string? lua)
		{
			int lines = 0;
			if (lua != null)
				foreach (var line in lua.EnumerateLines())
					lines++;

			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.LanguageLua,
				StatusTitle = LocalizationManager.LocalizeStaticFormat("lines_count", lines)
			};
		}

		public PreviewToolExecutionResult ExecutePreview(string lua)
		{
			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.LanguageLua,
				StatusTitle = null
			};
		}

		public ReactiveToolResult Execute(
			string lua,
			ToolExecutionContext context)
		{
			var reactiveResult = new ReactiveToolResult
			{
				StatusIcon = MaterialIconKind.LanguageLua,
				StatusTitle = null
			};

			_ = Task.Run(() =>
			{
				try
				{
					var scriptResult = _lua.Execute(lua, print => reactiveResult.ResultContentLines.Add(print), g =>
					{
						g["_dass_tool_ctx"] = UserData.Create(context);
						g["_dass_tool_result"] = UserData.Create(reactiveResult);
					});
					reactiveResult.ResultContentLines.Add($"Script returned: " + scriptResult.ToPrintString());
					reactiveResult.CompleteWithSuccess();
				}
				catch (Exception ex)
				{
					reactiveResult.ResultContentLines.Add("Caught error: " + ex.Message);
					reactiveResult.CompleteWithError();
				}
				finally
				{
				}
			}, CancellationToken.None);

			return reactiveResult;
		}
	}
}