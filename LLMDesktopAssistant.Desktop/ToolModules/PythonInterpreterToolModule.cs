using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LLMDesktopAssistant.Desktop.ToolModules.Terminal;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using Material.Icons;
using RCLargeLanguageModels.Tools;
using UglyToad.PdfPig.Graphics.Operations.PathPainting;
using XTerm.Common;

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
		private readonly Chat _chat;
		private readonly WorkingDirectoryAccessService _fileAccess;

		public PythonInterpreterToolModule(Chat chat, WorkingDirectoryAccessService fileAccess)
		{
			_chat = chat;
			_fileAccess = fileAccess;

			AddTool(Execute, ExecuteStreaming, ExecutePreview,
				new ToolInitializationInfo
				{
					Name = "execute-python",
					Description = "Executes Python in isolated virtual environment (global variables are not accessible between scripts) from the working directory. It returns STOUT of the executed code (e.g., print('Hello World!') should return 'Hello World!'). Displays live output in a terminal.",
					Category = "Python",
					DefaultExpectedBehaviour = ToolBehaviour.ExecuteExternalProcess | ToolBehaviour.PossiblyUnexpected | ToolBehaviour.RunTerminal
				});

			AddTool(ExecuteVenvShell, ExecuteVenvShellStreaming, ExecuteVenvShellPreview,
				new ToolInitializationInfo
				{
					Name = "execute-python_venv_shell",
					Description = "Executes shell script in a Python's virtual environment from the working directory. Useful for installing packages via 'pip'. Displays live output in a terminal.",
					Category = "Python",
					DefaultExpectedBehaviour = ToolBehaviour.ExecuteExternalProcess | ToolBehaviour.PossiblyUnexpected | ToolBehaviour.RunTerminal
				});

			AddTool(GetInstalledPackagesList,
				new ToolInitializationInfo
				{
					Name = "python-get_installed_packages_list",
					Description = "Returns the list of installed packages in the current Python's virtual environment.",
					Category = "Python",
					DefaultExpectedBehaviour = ToolBehaviour.ExecuteExternalProcess
				});
		}

		public StreamingToolArgumentsAnalysisResult ExecuteStreaming(string? python)
		{
			int lines = 0;
			if (python != null)
				foreach (var line in python.EnumerateLines())
					lines++;

			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.LanguagePython,
				StatusTitle = LocalizationManager.LocalizeStaticFormat("lines_count", lines)
			};
		}

		public PreviewToolExecutionResult ExecutePreview(string python, bool runTerminal)
		{
			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.LanguagePython,
				StatusTitle = null,
				ExpectedBehaviour = ToolBehaviour.ExecuteExternalProcess | ToolBehaviour.PossiblyUnexpected |
					(runTerminal ? ToolBehaviour.RunTerminal : 0)
			};
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
				python = File.ReadAllText(pyFile);

				// Ensure the script is UTF-8 encoded
				python = $"""
				import sys
				sys.stdout.reconfigure(encoding="utf-8")
				sys.stderr.reconfigure(encoding="utf-8")
				{python}
				""";

				// Write to a temporary file
				pyFile = Path.GetFullPath(Path.Combine(workDir, $"_dass_temp_{Guid.NewGuid()}.py"));
				File.WriteAllText(pyFile, python);
				isTemporaryFile = true;
			}
			else
			{
				// Ensure the script is UTF-8 encoded
				python = $"""
				import sys
				sys.stdout.reconfigure(encoding="utf-8")
				sys.stderr.reconfigure(encoding="utf-8")
				{python}
				""";

				// Write to a temporary file
				pyFile = Path.GetFullPath(Path.Combine(workDir, $"_dass_temp_{Guid.NewGuid()}.py"));
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
				var result = await RunAsync(new TerminalToolRunParameters
				{
					StatusIcon = MaterialIconKind.LanguagePython,
					StatusTitle = null,
					RunTerminal = runTerminal,
					Command = command,
					WorkingDirectory = workDir,
				}, context, cancellationToken);

				_ = result.Completion.ContinueWith(t =>
				{
					try
					{
						if (isTemporaryFile && File.Exists(pyFile))
							File.Delete(pyFile);
					}
					catch { }
				}, CancellationToken.None);

				return result;
			}
			catch (Exception ex)
			{
				try
				{
					if (isTemporaryFile && File.Exists(pyFile))
						File.Delete(pyFile);
				}
				catch { }

				return new ReactiveToolResult
				{
					ResultContent = "Error running Python script: " + ex.Message,
					StatusIcon = MaterialIconKind.LanguagePython,
					StatusTitle = null,
				}.CompleteWithError();
			}
		}

		public StreamingToolArgumentsAnalysisResult ExecuteVenvShellStreaming(string shell)
		{
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.LanguagePython,
				StatusTitle = $"`{shell}`"
			};
		}

		public PreviewToolExecutionResult ExecuteVenvShellPreview(string shell, bool runTerminal)
		{
			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.LanguagePython,
				StatusTitle = $"`{shell}`",
				ExpectedBehaviour = ToolBehaviour.ExecuteExternalProcess | ToolBehaviour.PossiblyUnexpected |
					(runTerminal ? ToolBehaviour.RunTerminal : 0)
			};
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
				command = $"{shell}";

			return RunAsync(new TerminalToolRunParameters
			{
				StatusIcon = MaterialIconKind.LanguagePython,
				StatusTitle = null,
				RunTerminal = runTerminal,
				Command = command,
				WorkingDirectory = workDir,
			}, context, cancellationToken);
		}

		/// <summary>
		/// Gets the list of installed packages in the current Python virtual environment.
		/// Still returns a simple ToolResult since it's a quick query, not a long-running process.
		/// </summary>
		public Task<ReactiveToolResult> GetInstalledPackagesList(
			ToolExecutionContext context, CancellationToken cancellationToken = default)
		{
			var workDir = _chat.Settings.Environment.GetWorkingDirectory();
			var venvPath = _chat.Settings.Environment.PythonVenvActivateScriptPath;

			string command;
			if (!string.IsNullOrWhiteSpace(venvPath))
				command = $"call \"{venvPath}\" && pip list";
			else
				command = $"pip list";

			return RunAsync(new TerminalToolRunParameters
			{
				StatusIcon = MaterialIconKind.LanguagePython,
				StatusTitle = null,
				RunTerminal = false,
				Command = command,
				WorkingDirectory = workDir,
			}, context, cancellationToken);
		}
	}
}
