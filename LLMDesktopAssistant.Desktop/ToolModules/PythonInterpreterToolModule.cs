using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Tools;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Desktop.ToolModules
{
	[ToolModule]
	public class PythonInterpreterToolModule : ToolModule
	{
		private readonly PythonService _python;
		private readonly Chat _chat;

		public PythonInterpreterToolModule(Chat chat)
		{
			_python = ServiceRegistry.Get<PythonService>();
			_chat = chat;

			AddTool(Execute,
				new ToolInitializationInfo
				{
					Name = "execute-python",
					Description = "Executes Python in isolated virtual environment (global variables are not accessible between scripts, use other, REPL tools for context sharing). It returns STOUT of the executed code (e.g., print('Hello World!') should return 'Hello World!').",
					Category = "Python",
					AskForConfirmation = true
				});

			AddTool(ExecuteVenvShell,
				new ToolInitializationInfo
				{
					Name = "execute-python_venv_shell",
					Description = "Executes Windows shell script in a Python's virtual environment. Useful for installing packages via 'pip'.",
					Category = "Python",
					AskForConfirmation = true
				});

			AddTool(GetInstalledPackagesList,
				new ToolInitializationInfo
				{
					Name = "python-get_installed_packages_list",
					Description = "Returns the list of installed packages in the current Python's virtual environment.",
					Category = "Python",
					AskForConfirmation = false
				});
		}

		public async Task<ToolResult> Execute(string python, CancellationToken cancellationToken = default)
		{
			try
			{
				var result = await _python.RunScript(python, _chat.Settings.Environment.GetWorkingDirectory(),
					_chat.Settings.Environment.PythonVenvActivateScriptPath, cancellationToken);

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
				var result = await _python.RunVenv(shell, _chat.Settings.Environment.GetWorkingDirectory(),
					_chat.Settings.Environment.PythonVenvActivateScriptPath, cancellationToken);

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
				var result = await _python.RunVenv("pip list", _chat.Settings.Environment.GetWorkingDirectory(),
					_chat.Settings.Environment.PythonVenvActivateScriptPath, cancellationToken);

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
	}
}