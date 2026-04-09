using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.ToolModules;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Tools;
using System.Text;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	public class PythonInterpreterToolModule : ToolModule
	{
		private readonly PythonModule _python;
		private readonly Chat _chat;

		public PythonInterpreterToolModule(Chat chat)
		{
			_python = ModuleManager.Get<PythonModule>();
			_chat = chat;

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(Execute, "execute-python",
					"Executes Python and returns STOUT of the executed code (e.g., print('Hello World!') should return 'Hello World!')."),
				Category = "scripting",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ExecuteVenvShell, "execute-python_venv_shell",
					"Executes Windows shell script in a Python's virtual environment. Useful for installing packages via 'pip'."),
				Category = "scripting",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(GetInstalledPackagesList, "python-get_installed_packages_list",
					"Returns the list of installed packages in the current Python's virtual environment."),
				Category = "scripting",
				AskForConfirmation = false
			});
		}

		public async Task<ToolResult> Execute(string python, CancellationToken cancellationToken = default)
		{
			try
			{
				var result = await _python.RunScript(python, _chat.Settings.GetWorkingDirectory(), cancellationToken);

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

		public async Task<ToolResult> ExecuteVenvShell(string shell, CancellationToken cancellationToken = default)
		{
			try
			{
				var result = await _python.RunVenv(shell, _chat.Settings.GetWorkingDirectory(), cancellationToken);

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

		public async Task<ToolResult> GetInstalledPackagesList(CancellationToken cancellationToken = default)
		{
			try
			{
				var result = await _python.RunVenv("pip list", _chat.Settings.GetWorkingDirectory(), cancellationToken);
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