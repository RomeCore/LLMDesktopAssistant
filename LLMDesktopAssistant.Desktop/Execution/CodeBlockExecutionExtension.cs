using CommunityToolkit.Mvvm.Input;
using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.UIExtensions.CodeBlockExtensions;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.LLM.Domain;
using Material.Icons;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LLMDesktopAssistant.Desktop.Execution
{
	[CodeBlockExtension]
	public class CodeBlockExecutionExtension : CodeBlockExtension
	{
		public override MaterialIconKind Icon => MaterialIconKind.Play;

		public override ICommand Command { get; }

		public CodeBlockExecutionExtension(CodeBlock codeBlock, Chat chat, LuaService lua, PythonService python)
		{
			var command = new AsyncRelayCommand(async () =>
			{
				await Execute(codeBlock, chat, lua, python);
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
				"lua" => true,
				"python" or "py" => true,
				"shell" => true,
				"powerhell" or "ps1" => true,
				"bash" or "sh" => true,
				_ => false
			};
		}

		private async Task Execute(CodeBlock codeBlock, Chat chat, LuaService lua, PythonService python)
		{
			var code = codeBlock.Code;
			if (string.IsNullOrEmpty(code))
				return;
			var workDir = chat.Settings.Environment.GetWorkingDirectory();
			var pythonVenvScript = chat.Settings.Environment.PythonVenvActivateScriptPath;

			switch (codeBlock.Language)
			{
				case "lua":

					StringBuilder sb = new();
					lua.Execute(code, out var print);
					foreach (var line in print)
						sb.AppendLine(line);
					AdditionalViewModel = sb.ToString();

					break;

				case "python":
				case "py":

					sb = new();
					var result = await python.RunScript(code, workDir, pythonVenvScript);
					sb.Append(result.StdOut);
					if (!string.IsNullOrWhiteSpace(result.StdErr))
						sb.AppendLine().AppendLine().AppendLine("Errors:").Append(result.StdErr);
					AdditionalViewModel = sb.ToString();

					break;

				case "shell":

					sb = new();
					result = await ShellExecutor.ExecuteWindowsScriptAsync(code, workDir);
					sb.Append(result.StdOut);
					if (!string.IsNullOrWhiteSpace(result.StdErr))
						sb.AppendLine().AppendLine().AppendLine("Errors:").Append(result.StdErr);
					AdditionalViewModel = sb.ToString();

					break;

				case "powerhell":
				case "ps1":

					sb = new();
					result = await ShellExecutor.ExecuteWindowsPSScriptAsync(code, workDir);
					sb.Append(result.StdOut);
					if (!string.IsNullOrWhiteSpace(result.StdErr))
						sb.AppendLine().AppendLine().AppendLine("Errors:").Append(result.StdErr);
					AdditionalViewModel = sb.ToString();

					break;

				case "bash":
				case "sh":

					sb = new();
					result = await ShellExecutor.ExecuteBashScriptAsync(code, workDir);
					sb.Append(result.StdOut);
					if (!string.IsNullOrWhiteSpace(result.StdErr))
						sb.AppendLine().AppendLine().AppendLine("Errors:").Append(result.StdErr);
					AdditionalViewModel = sb.ToString();

					break;
			}
		}
	}
}