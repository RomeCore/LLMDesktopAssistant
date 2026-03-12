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
		private PythonModule _python = null!;

		public PythonInterpreterToolModule()
		{
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(Execute, "execute-python", "Executes Python and returns the script result."),
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ExecuteVenvShell, "execute-python_venv_shell", "Executes Windows shell script in a Python's virtual environment. Useful for installing packages via 'pip'."),
				AskForConfirmation = true
			});
		}

		public override void Initialize()
		{
			_python = ModuleManager.Get<PythonModule>();
		}

		public async Task<ToolResult> Execute(string python)
		{
			try
			{
				var result = await _python.RunScript(python);

				var resultBuilder = new StringBuilder();
				resultBuilder.Append(result.StdOut);
				if (!string.IsNullOrEmpty(result.StdErr))
				{
					resultBuilder.AppendLine().AppendLine("Errors:");
					resultBuilder.Append(result.StdErr);
				}

				var status = result.Success ? ToolResultStatus.Success : ToolResultStatus.Error;
				return new ToolResult(status, resultBuilder.ToString());
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Got error while executing Python: {ex.Message}");
			}
		}

		public async Task<ToolResult> ExecuteVenvShell(string shellScript)
		{
			try
			{
				var result = await _python.RunVenv(shellScript);

				var resultBuilder = new StringBuilder();
				resultBuilder.Append(result.StdOut);
				if (!string.IsNullOrEmpty(result.StdErr))
				{
					resultBuilder.AppendLine().AppendLine("Errors:");
					resultBuilder.Append(result.StdErr);
				}

				var status = result.Success ? ToolResultStatus.Success : ToolResultStatus.Error;
				return new ToolResult(status, resultBuilder.ToString());
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Got error while executing Lua: {ex.Message}");
			}
		}
	}
}