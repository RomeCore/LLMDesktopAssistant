using System.Text;
using LLMDesktopAssistant.Tools;
using MoonSharp.Interpreter;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Scripting
{
	[ToolModule]
	public class LuaInterpreterToolModule : ToolModule
	{
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
						{string.Join(", ", lua.Namespaces.Select(ns => ns ?? "*global namespace*").Order())}
						
						Use the `manuals(...)` function to get the documentation for a specific namespace, `print(manuals())` or `print(manuals(dass.tools))` for example.
						Its very recommended to see manuals before starting to use the API (do not use it blindly without reading the documentation!).
						""",
					Category = "scripting",
					AskForConfirmation = true
				});
		}

		public ToolResult ExecuteLua(
			string lua,
			ToolExecutionContext context)
		{
			var printOutput = new List<string>();

			try
			{
				var scriptResult = _lua.Execute(lua, printOutput, g =>
				{
					g["_dass_tec"] = UserData.Create(context);
				});

				var resultBuilder = new StringBuilder();
				foreach (var message in printOutput)
					resultBuilder.AppendLine(message);
				resultBuilder.Append($"Script successfully returned: {scriptResult}");

				return new ToolResult(resultBuilder.ToString());
			}
			catch (Exception ex)
			{
				return new ToolResult($"Got error while executing Lua: {ex.Message}");
			}
		}
	}
}