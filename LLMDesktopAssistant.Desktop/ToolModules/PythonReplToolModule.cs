using DocumentFormat.OpenXml.Wordprocessing;
using LLMDesktopAssistant.Desktop.Execution.Python;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Tools;
using ReverseMarkdown.Converters;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Desktop.ToolModules
{
	// TODO: Make REPL stable
	// [ToolModule]
	public class PythonReplToolModule : ToolModule
	{
		private readonly PythonReplService _python;

		public PythonReplToolModule(PythonReplService python)
		{
			_python = python;

			AddTool(ExecutePython,
				new ToolInitializationInfo
				{
					Name = "execute-python_repl",
					Description = "Execute Python code in the persistent REPL. Context is preserved between calls! It returns STOUT of the executed code (e.g., print('Hello World!') should return 'Hello World!').",
					Category = "Python",
					AskForConfirmation = true
				});

			AddTool(GetPythonVariable,
				new ToolInitializationInfo
				{
					Name = "python-repl_get_var",
					Description = "Get a Python variable value from the persistent REPL.",
					Category = "Python"
				});

			AddTool(ResetPythonRepl,
				new ToolInitializationInfo
				{
					Name = "python-repl_reset",
					Description = "Reset/clear the Python REPL context.",
					Category = "Python"
				});
		}

		public async Task<ToolResult> ExecutePython(string code, CancellationToken cancellationToken = default)
		{
			try
			{
				var result = await _python.ExecuteAsync(code, cancellationToken);
				return new ToolResult(result);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error executing Python REPL code: {ex.Message}");
			}
		}

		public async Task<ToolResult> GetPythonVariable(string name, CancellationToken cancellationToken = default)
		{
			try
			{
				var result = await _python.GetVariableAsync(name, cancellationToken);
				return new ToolResult(result);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error getting Python variable '{name}': {ex.Message}");
			}
		}

		public async Task<ToolResult> ResetPythonRepl(CancellationToken cancellationToken = default)
		{
			try
			{
				await _python.ResetAsync(cancellationToken);
				return new ToolResult("Python REPL context reset successfully.");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error resetting Python REPL: {ex.Message}");
			}
		}
	}
}