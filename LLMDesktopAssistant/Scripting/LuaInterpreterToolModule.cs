using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonSharp.Interpreter;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Tools;
using System.Collections.Concurrent;

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
					Description = "Executes Lua and returns the script result along with messages printed by 'print' function.",
					Category = "scripting",
					AskForConfirmation = true
				});
		}

		public ToolResult ExecuteLua(string lua)
		{
			try
			{
				var scriptResult = _lua.Execute(lua, out var printOutput);

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