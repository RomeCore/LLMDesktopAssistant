using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.ToolModules;
using Python.Runtime;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Scripting
{
	[Module]
	public class PythonInterpreterToolModule : ToolModule
	{
		private readonly List<FunctionTool> _tools;
		private PythonModule _python = null!;

		public PythonInterpreterToolModule()
		{
			_tools = [];
			_tools.Add(FunctionTool.From(Execute, "execute-python", "Executes Python and returns the script result."));
			_tools.Add(FunctionTool.From(ExecuteVenvShell, "execute-python_venv_shell", "Executes Windows shell script in a Python's virtual environment. Useful for installing packages via 'pip'."));
		}

		public override void Initialize()
		{
			_python = ModuleManager.Get<PythonModule>();
		}

		public async Task<ToolResult> Execute(string python)
		{
			try
			{
				var (stdout, stderr) = await _python.RunScript(python);

				var resultBuilder = new StringBuilder();
				resultBuilder.Append(stdout);
				if (stderr != null)
				{
					resultBuilder.AppendLine().AppendLine("Errors:");
					resultBuilder.Append(stderr);
				}

				return new ToolResult(resultBuilder.ToString());
			}
			catch (Exception ex)
			{
				return new ToolResult($"Got error while executing Python: {ex.Message}");
			}
		}

		public async Task<ToolResult> ExecuteVenvShell(string shellScript)
		{
			try
			{
				var (stdout, stderr) = await _python.RunVenv(shellScript);

				var resultBuilder = new StringBuilder();
				resultBuilder.Append(stdout);
				if (stderr != null)
				{
					resultBuilder.AppendLine().AppendLine("Errors:");
					resultBuilder.Append(stderr);
				}

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