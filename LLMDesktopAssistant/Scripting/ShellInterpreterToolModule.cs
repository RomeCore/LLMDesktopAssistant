using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.ToolModules;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Scripting
{
	[Module]
	public class ShellInterpreterToolModule : ToolModule
	{
		public ShellInterpreterToolModule()
		{
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ExecuteShell, "execute-shell", "Executes Windows shell script."),
				AskForConfirmation = true
			});
		}

		public async Task<ToolResult> ExecuteShell(string shell)
		{
			try
			{
				var result = await ShellExecutor.ExecuteAsync(shell);

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
				return new ToolResult(ToolResultStatus.Error, $"Got error while executing Shell: {ex.Message}");
			}
		}
	}
}