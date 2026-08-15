using System.Collections.Immutable;
using System.IO;
using LLMDesktopAssistant.Desktop.Execution;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Desktop.Scripting.Python
{
	[Service]
	public class PythonHelperService
	{
		public ProcessLaunchParameters CreateLaunchParameters(ChatEnvironmentSettings envSettings, string command,
			string? processName, bool runInTerminal, bool isMetaTool)
		{
			var workDir = envSettings.GetEffectiveWorkingDirectories().GetWorkingDirectory();
			var pythonConfig = envSettings.EnsureAdditional<PythonEnvironmentConfiguration>();
			var venvPath = isMetaTool
				? (pythonConfig.PythonMetaVenvActivateScriptPath ?? pythonConfig.PythonVenvActivateScriptPath)
				: pythonConfig.PythonVenvActivateScriptPath;

			string interpreter = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash";

			if (!string.IsNullOrWhiteSpace(venvPath))
			{
				var venvExtension = Path.GetExtension(venvPath);
				switch (venvExtension)
				{
					case ".bat":
						interpreter = "cmd.exe";
						command = $"call \"{venvPath}\" && {command}";
						break;
					case ".sh":
						interpreter = "/bin/bash";
						command = $"\"{venvPath}\" && {command}";
						break;
					case ".ps1":
						interpreter = "powershell";
						command = $"-ExecutionPolicy Bypass -File \"{venvPath}\"; {command}";
						break;
					default:
						if (OperatingSystem.IsWindows())
							command = $"call \"{venvPath}\" && {command}";
						else
							command = $"\"{venvPath}\" && {command}";
						break;
				}
			}

			ImmutableList<string> arguments = interpreter switch
			{
				"cmd.exe" => ["/c", $"\"{command}\""],
				"/bin/bash" => ["-c", command],
				"powershell" => ["-Command", command],
				_ => throw new InvalidOperationException("Unsupported interpreter")
			};
			bool verbatimArguments = interpreter == "cmd.exe";

			return new ProcessLaunchParameters
			{
				ProcessName = processName,
				RunInTerminal = runInTerminal,
				FileName = interpreter,
				Arguments = arguments,
				VerbatimArguments = verbatimArguments,
				WorkingDirectory = workDir
			};
		}
	}
}
