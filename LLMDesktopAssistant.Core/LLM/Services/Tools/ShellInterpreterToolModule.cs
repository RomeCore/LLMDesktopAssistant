using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.Services;
using LLMDesktopAssistant.Core.Scripting;
using LLMDesktopAssistant.Core.ToolModules;
using LLMDesktopAssistant.Core.Utils;
using RCLargeLanguageModels.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.LLM.Services.Tools
{
	public class ShellInterpreterToolModule : ToolModule
	{
		private readonly Chat _chat;

		public ShellInterpreterToolModule(Chat chat)
		{
			_chat = chat;

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ExecuteWindows,
					"execute-shell",
					"Executes Windows shell (.bat) script."),
				Category = "scripting",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ExecuteWindowsPS,
					"execute-powershell",
					"Executes Windows Powershell (.ps1) script."),
				Category = "scripting",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ExecuteLinux,
					"execute-bash",
					"Executes Linux bash (.sh) script."),
				Category = "scripting",
				AskForConfirmation = true
			});
		}

		public async Task<ToolResult> ExecuteWindows(string shell)
		{
			try
			{
				var workDir = _chat.Settings.GetWorkingDirectory();
				var result = shell.Contains('\n') ?
					await ShellExecutor.ExecuteWindowsScriptAsync(shell, workDir) :
					await ShellExecutor.ExecuteWindowsAsync(shell, workDir);

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
				return new ToolResult(ToolResultStatus.Error, $"Got error while executing Windows shell: {ex.Message}");
			}
		}

		public async Task<ToolResult> ExecuteWindowsPS(string powershell)
		{
			try
			{
				var workDir = _chat.Settings.GetWorkingDirectory();
				var result = powershell.Contains('\n') ?
					await ShellExecutor.ExecuteWindowsPSScriptAsync(powershell, workDir) :
					await ShellExecutor.ExecuteWindowsPSAsync(powershell, workDir);

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
				return new ToolResult(ToolResultStatus.Error, $"Got error while executing Windows shell: {ex.Message}");
			}
		}

		public async Task<ToolResult> ExecuteLinux(string bash)
		{
			try
			{
				var workDir = _chat.Settings.GetWorkingDirectory();
				var result = bash.Contains('\n') ?
					await ShellExecutor.ExecuteBashScriptAsync(bash, workDir) :
					await ShellExecutor.ExecuteBashAsync(bash, workDir);

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
				return new ToolResult(ToolResultStatus.Error, $"Got error while executing Linux shell: {ex.Message}");
			}
		}
	}
}