using LLMDesktopAssistant.Desktop.ToolModules.Terminal;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Tools;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Desktop.ToolModules
{
	[ToolModule]
	public class ShellInterpreterToolModule : TerminalBasedToolModule
	{
		private readonly Chat _chat;

		public ShellInterpreterToolModule(Chat chat)
		{
			_chat = chat;

			var osName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
						 RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" :
						 RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" :
						 RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) ? "FreeBSD" : "Unknown";

			AddTool(ExecuteShell,
				new ToolInitializationInfo
				{
					Name = "execute-shell",
					Description = $"Executes {osName} shell command or script from the current working directory. Examples: `git status`, `python script.py`",
					Category = "scripting",
					AskForConfirmation = true
				});

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				AddTool(ExecutePowerShell,
					new ToolInitializationInfo
					{
						Name = "execute-powershell",
						Description = "Executes Windows powershell command or script from the current working directory.",
						Category = "scripting",
						AskForConfirmation = true
					});
			}
		}



		public Task<ReactiveToolResult> ExecuteShell(
			[Description("The shell command to run.")] string shell,
			[Description("Whether to run the output in an embedded terminal emulator. Use `true` for long-running scripts.")] bool runTerminal,
			ToolExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			var workDir = _chat.Settings.Environment.GetWorkingDirectory();
			string command = shell;

			return RunAsync(new TerminalRunParameters
			{
				RunTerminal = runTerminal,
				Command = command,
				WorkingDirectory = workDir,
			}, context, cancellationToken);
		}

		public Task<ReactiveToolResult> ExecutePowerShell(
			[Description("The powershell command to run.")] string powershell,
			[Description("Whether to run the output in an embedded terminal emulator. Use `true` for long-running scripts.")] bool runTerminal,
			ToolExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			var workDir = _chat.Settings.Environment.GetWorkingDirectory();
			string command = $"powershell -Command \"{powershell}\"";

			return RunAsync(new TerminalRunParameters
			{
				RunTerminal = runTerminal,
				Command = command,
				WorkingDirectory = workDir,
			}, context, cancellationToken);
		}
	}
}