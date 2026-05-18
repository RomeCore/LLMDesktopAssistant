using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Tools;
using System;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Desktop.ToolModules
{
	[ToolModule]
	public class ShellInterpreterToolModule : ToolModule
	{
		private readonly Chat _chat;

		public ShellInterpreterToolModule(Chat chat)
		{
			_chat = chat;

			AddTool(ExecuteWindows,
				new ToolInitializationInfo
				{
					Name = "execute-shell",
					Description = "Executes Windows shell (.bat) script.",
					Category = "scripting",
					AskForConfirmation = true
				});

			AddTool(ExecuteWindowsPS,
				new ToolInitializationInfo
				{
					Name = "execute-powershell",
					Description = "Executes Windows Powershell (.ps1) script.",
					Category = "scripting",
					AskForConfirmation = true
				});

			AddTool(ExecuteLinux,
				new ToolInitializationInfo
				{
					Name = "execute-bash",
					Description = "Executes Linux bash (.sh) script.",
					Category = "scripting",
					AskForConfirmation = true
				});
		}

		public async Task<ToolResult> ExecuteWindows(string shell)
		{
			try
			{
				var workDir = _chat.Settings.Environment.GetWorkingDirectory();
				var result = shell.Contains('\n') ?
					await ShellExecutor.ExecuteWindowsScriptAsync(shell, workDir) :
					await ShellExecutor.ExecuteWindowsAsync(shell, workDir);

				var resultBuilder = new StringBuilder();
				resultBuilder.Append(result.StdOut);
				if (!string.IsNullOrEmpty(result.StdErr))
				{
					resultBuilder.AppendLine().AppendLine("STDERR:");
					resultBuilder.Append(result.StdErr);
				}

				var status = result.Success ? ToolResultStatus.Success : ToolResultStatus.Error;
				return new ToolResult(status, resultBuilder.ToString());
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Got error while executing Windows shell: {ex.Message}");
			}
		}

		public async Task<ToolResult> ExecuteWindowsPS(string powershell)
		{
			try
			{
				var workDir = _chat.Settings.Environment.GetWorkingDirectory();
				var result = powershell.Contains('\n') ?
					await ShellExecutor.ExecuteWindowsPSScriptAsync(powershell, workDir) :
					await ShellExecutor.ExecuteWindowsPSAsync(powershell, workDir);

				var resultBuilder = new StringBuilder();
				resultBuilder.Append(result.StdOut);
				if (!string.IsNullOrEmpty(result.StdErr))
				{
					resultBuilder.AppendLine().AppendLine("STDERR:");
					resultBuilder.Append(result.StdErr);
				}

				var status = result.Success ? ToolResultStatus.Success : ToolResultStatus.Error;
				return new ToolResult(status, resultBuilder.ToString());
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Got error while executing Windows shell: {ex.Message}");
			}
		}

		public async Task<ToolResult> ExecuteLinux(string bash)
		{
			try
			{
				var workDir = _chat.Settings.Environment.GetWorkingDirectory();
				var result = bash.Contains('\n') ?
					await ShellExecutor.ExecuteBashScriptAsync(bash, workDir) :
					await ShellExecutor.ExecuteBashAsync(bash, workDir);

				var resultBuilder = new StringBuilder();
				resultBuilder.Append(result.StdOut);
				if (!string.IsNullOrEmpty(result.StdErr))
				{
					resultBuilder.AppendLine().AppendLine("STDERR:");
					resultBuilder.Append(result.StdErr);
				}

				var status = result.Success ? ToolResultStatus.Success : ToolResultStatus.Error;
				return new ToolResult(status, resultBuilder.ToString());
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Got error while executing Linux shell: {ex.Message}");
			}
		}
	}
}