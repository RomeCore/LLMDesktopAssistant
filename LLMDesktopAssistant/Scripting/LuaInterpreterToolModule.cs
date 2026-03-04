using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonSharp.Interpreter;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.ToolModules;
using RCLargeLanguageModels.Tools;
using System.Collections.Concurrent;

namespace LLMDesktopAssistant.Scripting
{
	[Module]
	public class LuaInterpreterToolModule : ToolModule
	{
		private readonly List<FunctionTool> _tools;
		private LuaModule _lua = null!;

		public LuaInterpreterToolModule()
		{
			_tools = [];
			_tools.Add(FunctionTool.From(ExecuteLua, "execute-lua", "Executes Lua and returns the script result along with messages printed by 'print' function."));
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

		public override IEnumerable<ITool> GetTools()
		{
			return _tools;
		}
	}
}