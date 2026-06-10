using CommunityToolkit.Mvvm.Input;
using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.UIExtensions.CodeBlockExtensions;
using LLMDesktopAssistant.LLM.Domain;
using Material.Icons;
using System.Threading.Tasks;
using System.Windows.Input;
using LLMDesktopAssistant.Desktop.Execution.Python;
using System;

namespace LLMDesktopAssistant.Desktop.Execution
{
	[CodeBlockExtension]
	public class CodeBlockPythonREPLExecutionExtension : CodeBlockExtension
	{
		public CodeBlockPythonREPLExecutionExtension(CodeBlock codeBlock, Chat chat, PythonReplService python)
		{
			Icon = MaterialIconKind.BookPlay;

			Tooltip = "execute_using_repl";

			var command = new AsyncRelayCommand(async () =>
			{
				await Execute(codeBlock, chat, python);
			}, () =>
			{
				return CanExecute(codeBlock);
			});
			Command = command;

			codeBlock.PropertyChanged += (s, e) =>
			{
				if (e.Property == CodeBlock.LanguageProperty)
				{
					command.NotifyCanExecuteChanged();
					IsButtonVisible = CanExecute(codeBlock);
				}
			};
		}

		private bool CanExecute(CodeBlock codeBlock)
		{
			return codeBlock.Language switch
			{
				"python" or "py" => true,
				_ => false
			};
		}

		private async Task Execute(CodeBlock codeBlock, Chat chat, PythonReplService python)
		{
			var code = codeBlock.Code;
			if (string.IsNullOrEmpty(code))
				return;

			try
			{
				var result = await python.ExecuteAsync(code);
				AdditionalViewModel = new ExecutionResultViewModel
				{
					OutputText = result
				};
			}
			catch (Exception ex)
			{
				AdditionalViewModel = new ExecutionResultViewModel
				{
					OutputText = ex.Message,
					IsError = true
				};
			}
		}
	}
}