using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonSharp.Interpreter;
using LLMDesktopAssistant.Core.Modules;
using LLMDesktopAssistant.Core.ToolModules;
using RCLargeLanguageModels.Tools;
using System.Collections.Concurrent;

namespace LLMDesktopAssistant.Core.Scripting
{
	[Module]
	public class LuaInterpreterToolModule : ToolModule
	{
		private LuaModule _lua = null!;

		public LuaInterpreterToolModule()
		{
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ExecuteLua, "execute-lua", "Executes Lua and returns the script result along with messages printed by 'print' function."),
				Category = "scripting",
				AskForConfirmation = true
			});
		}

		public override void Initialize()
		{
			_lua = ModuleManager.Get<LuaModule>();
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