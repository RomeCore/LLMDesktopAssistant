using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.ToolModules;
using MaterialDesignThemes.Wpf;
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

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(GetInstalledPackagesList, "execute-python-get_installed_packages_list", "Returns the list of installed packages in the current Python's virtual environment."),
				AskForConfirmation = false
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

		public async Task<ToolResult> ExecuteVenvShell(string shell)
		{
			try
			{
				var result = await _python.RunVenv(shell);

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
				return new ToolResult(ToolResultStatus.Error, $"Got error while executing Python shell script in a virtual environment: {ex.Message}");
			}
		}

		public async Task<ToolResult> GetInstalledPackagesList()
		{
			try
			{
				var result = await _python.RunVenv("pip list");
				var status = result.Success ? ToolResultStatus.Success : ToolResultStatus.Error;
				return new ToolResult(status, result.StdOut);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Got error while executing Python shell script in a virtual environment: {ex.Message}");
			}
		}
	}
}