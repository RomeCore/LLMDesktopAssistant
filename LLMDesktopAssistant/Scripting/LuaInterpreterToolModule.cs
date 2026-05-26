using System.Text;
using LLMDesktopAssistant.Tools;
using MoonSharp.Interpreter;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Scripting
{
	[ToolModule]
	public class LuaInterpreterToolModule : ToolModule
	{
		private readonly SemaphoreSlim _semaphore = new(1, 1);
		private readonly LuaService _lua;

		public LuaInterpreterToolModule(LuaService lua)
		{
			_lua = lua;

			AddTool(ExecuteLua,
				new ToolInitializationInfo
				{
					Name = "execute-lua",
					DescriptionGetter = () => $"""
						Executes Lua and returns the script result along with messages printed by 'print' function.
						
						Lua has the API to interact with the application (called dASS) with these namespaces:
						{string.Join(", ", lua.Namespaces.Select(ns => ns != null ? $"**{ns}**" : "*global namespace*").Order())}
						
						Use the `manuals(...)` function to get the documentation for a specific namespace, `print(manuals())` or `print(manuals(dass.tools))` for example.
						Its very recommended to see manuals before starting to use the API (do not use it blindly without reading the documentation!).
						""",
					Category = "scripting",
					AskForConfirmation = true
				});
		}

		public ReactiveToolResult ExecuteLua(
			string lua,
			ToolExecutionContext context)
		{
			var reactiveResult = new ReactiveToolResult();

			_ = Task.Run(() =>
			{
				_semaphore.Wait();
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
					_semaphore.Release();
				}
			}, CancellationToken.None);

			return reactiveResult;
		}
	}
}