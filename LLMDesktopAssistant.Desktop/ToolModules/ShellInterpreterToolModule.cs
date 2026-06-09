using LLMDesktopAssistant.Desktop.ToolModules.Terminal;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Tools;
using Material.Icons;
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
						 RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) ? "FreeBSD" :
						 "Unknown";

			AddTool(ExecuteShell, ExecuteShellStreaming, ExecuteShellPreview,
				new ToolInitializationInfo
				{
					Name = "execute-shell",
					Description = $"Executes `{osName}` shell command or script from the current working directory. Examples: `git status`, `python script.py`",
					Category = "scripting",
					AskForConfirmation = true,
					DefaultDangerLevel = ToolDangerLevel.Dangerous
				});

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				AddTool(ExecutePowerShell, ExecutePowerShellStreaming, ExecutePowerShellPreview,
					new ToolInitializationInfo
					{
						Name = "execute-powershell",
						Description = "Executes Windows powershell command or script from the current working directory.",
						Category = "scripting",
						AskForConfirmation = true,
						DefaultDangerLevel = ToolDangerLevel.Dangerous
					});
			}
		}

		public StreamingToolArgumentsAnalysisResult ExecuteShellStreaming(string shell)
		{
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.Console,
				StatusTitle = $"`{shell}`"
			};
		}

		public PreviewToolExecutionResult ExecuteShellPreview(string shell)
		{
			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.Console,
				StatusTitle = $"`{shell}`"
			};
		}

		public Task<ReactiveToolResult> ExecuteShell(
			[Description("The shell command to run.")] string shell,
			[Description("Whether to run the output in an embedded terminal emulator. Use `true` for long-running scripts.")] bool runTerminal,
			ToolExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			var workDir = _chat.Settings.Environment.GetWorkingDirectory();
			string command = shell;

			return RunAsync(new TerminalToolRunParameters
			{
				StatusIcon = MaterialIconKind.Console,
				StatusTitle = $"`{shell}`",
				RunTerminal = runTerminal,
				Command = command,
				WorkingDirectory = workDir,
			}, context, cancellationToken);
		}

		public StreamingToolArgumentsAnalysisResult ExecutePowerShellStreaming(string powershell)
		{
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.Powershell,
				StatusTitle = $"`{powershell}`"
			};
		}

		public PreviewToolExecutionResult ExecutePowerShellPreview(string powershell)
		{
			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.Powershell,
				StatusTitle = $"`{powershell}`"
			};
		}

		public Task<ReactiveToolResult> ExecutePowerShell(
			[Description("The powershell command to run.")] string powershell,
			[Description("Whether to run the output in an embedded terminal emulator. Use `true` for long-running scripts.")] bool runTerminal,
			ToolExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			var workDir = _chat.Settings.Environment.GetWorkingDirectory();
			string command = $"powershell -Command \"{powershell}\"";

			return RunAsync(new TerminalToolRunParameters
			{
				StatusIcon = MaterialIconKind.Powershell,
				StatusTitle = $"`{powershell}`",
				RunTerminal = runTerminal,
				Command = command,
				WorkingDirectory = workDir,
			}, context, cancellationToken);
		}
	}
}