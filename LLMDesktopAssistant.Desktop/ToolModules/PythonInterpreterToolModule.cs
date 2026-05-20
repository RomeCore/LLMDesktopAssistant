using LLMDesktopAssistant.Desktop.ToolModules.Terminal;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels.Tools;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Desktop.ToolModules
{
	/// <summary>
	/// Terminal-based Python execution tool module.
	/// Runs Python scripts and displays live output in an embedded terminal emulator.
	/// Replaces PythonInterpreterToolModule with terminal UI support.
	/// </summary>
	[ToolModule]
	public class PythonInterpreterToolModule : TerminalBasedToolModule
	{
		private readonly PythonService _python;
		private readonly Chat _chat;
		private readonly FileAccessService _fileAccess;

		public PythonInterpreterToolModule(Chat chat, FileAccessService fileAccess)
		{
			_python = ServiceRegistry.Get<PythonService>();
			_chat = chat;
			_fileAccess = fileAccess;

			AddTool(Execute,
				new ToolInitializationInfo
				{
					Name = "execute-python",
					Description = "Executes Python in isolated virtual environment (global variables are not accessible between scripts) from the working directory. It returns STOUT of the executed code (e.g., print('Hello World!') should return 'Hello World!'). Displays live output in a terminal.",
					Category = "Python",
					AskForConfirmation = true
				});

			AddTool(ExecuteVenvShell,
				new ToolInitializationInfo
				{
					Name = "execute-python_venv_shell",
					Description = "Executes shell script in a Python's virtual environment from the working directory. Useful for installing packages via 'pip'. Displays live output in a terminal.",
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

		/// <summary>
		/// Executes a Python script in the terminal with live output.
		/// Creates a temporary .py file, runs it via the virtual environment's Python,
		/// and displays the output in an embedded terminal.
		/// </summary>
		public async Task<ReactiveToolResult> Execute(
			[Description("The Python code to execute or the path to *.py file.")] string python,
			[Description("Whether to run the output in an embedded terminal emulator. Use `true` for long-running scripts.")] bool runTerminal,
			ToolExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			var workDir = _chat.Settings.Environment.GetWorkingDirectory();
			var venvPath = _chat.Settings.Environment.PythonVenvActivateScriptPath;

			string pyFile;
			bool isTemporaryFile;
			if (!python.Contains('\n') && !python.Contains('\r') && python.EndsWith(".py"))
			{
				// Treat as a file path
				pyFile = _fileAccess.AccessPath(python);
				isTemporaryFile = false;
			}
			else
			{
				// Write to a temporary file
				pyFile = Path.GetFullPath(Path.Combine(workDir, $"{Guid.NewGuid()}.py"));
				File.WriteAllText(pyFile, python);
				isTemporaryFile = true;
			}

			try
			{
				// Build command: activate venv if available, then run python
				string command;
				if (!string.IsNullOrWhiteSpace(venvPath))
					command = $"call \"{venvPath}\" && python \"{pyFile}\"";
				else
					command = $"python \"{pyFile}\"";

				// Run in terminal
				return await RunAsync(new TerminalRunParameters
				{
					RunTerminal = runTerminal,
					Command = command,
					WorkingDirectory = workDir,
				}, context, cancellationToken);
			}
			finally
			{
				// Clean up temp file after process completes
				try
				{
					if (isTemporaryFile && File.Exists(pyFile))
						File.Delete(pyFile);
				}
				catch
				{
					// Ignore cleanup errors
				}
			}
		}

		/// <summary>
		/// Executes a shell command in the Python virtual environment via terminal.
		/// Useful for pip install, pip list, etc.
		/// </summary>
		public Task<ReactiveToolResult> ExecuteVenvShell(
			[Description("The shell command to run in the Python's virtual environment.")] string shell,
			[Description("Whether to run the output in an embedded terminal emulator. Use `true` for long-running scripts.")] bool runTerminal,
			ToolExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			var workDir = _chat.Settings.Environment.GetWorkingDirectory();
			var venvPath = _chat.Settings.Environment.PythonVenvActivateScriptPath;

			string command;
			if (!string.IsNullOrWhiteSpace(venvPath))
				command = $"call \"{venvPath}\" && {shell}";
			else
				command = shell;

			return RunAsync(new TerminalRunParameters
			{
				RunTerminal = runTerminal,
				Command = command,
				WorkingDirectory = workDir,
			}, context, cancellationToken);
		}

		/// <summary>
		/// Gets the list of installed packages in the current Python virtual environment.
		/// Still returns a simple ToolResult since it's a quick query, not a long-running process.
		/// </summary>
		public async Task<ToolResult> GetInstalledPackagesList(CancellationToken cancellationToken = default)
		{
			try
			{
				var result = await _python.RunVenv("pip list",
					_chat.Settings.Environment.GetWorkingDirectory(),
					_chat.Settings.Environment.PythonVenvActivateScriptPath,
					cancellationToken);

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
				return new ToolResult(ToolResultStatus.Error,
					$"Got error while executing Python shell script in a virtual environment: {ex.Message}");
			}
		}
	}
}
