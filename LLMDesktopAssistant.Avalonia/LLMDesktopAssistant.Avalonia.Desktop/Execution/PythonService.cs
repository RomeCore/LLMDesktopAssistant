using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LLMDesktopAssistant.Core.Services;
using LLMDesktopAssistant.Core.Utils;
using MoonSharp.Interpreter;
using Serilog;

namespace LLMDesktopAssistant.Core.Scripting
{
	[Service(Order = int.MinValue)]
	public class PythonService
	{
		/// <summary>
		/// Executes a Python script.
		/// </summary>
		/// <param name="script">The Python script to execute.</param>
		/// <param name="workDir">The working directory for the script.</param>
		/// <param name="venvActivatePath">The path to the 'activate' script for the virtual environment.</param>
		/// <returns>The result of the script execution.</returns>
		public async Task<ProcessExecutionResult> RunScript(string script, string workDir, string? venvActivatePath,
			CancellationToken cancellationToken = default)
		{
			script = $"""
				import sys
				sys.stdout.reconfigure(encoding="utf-8")
				sys.stderr.reconfigure(encoding="utf-8")
				
				{script}
				""";

			var tempPyFile = Path.GetFullPath(Path.Combine(Directories.TempScripts, $"{Guid.NewGuid()}.py"));
			File.WriteAllText(tempPyFile, script);

			try
			{
				string cmd;
				if (!string.IsNullOrWhiteSpace(venvActivatePath))
					cmd = $"call \"{venvActivatePath}\" && python \"{tempPyFile}\"";
				else
					cmd = $"python \"{tempPyFile}\"";
				return await ShellExecutor.ExecuteWindowsAsync(cmd, workDir: workDir, cancellationToken);
			}
			finally
			{
				File.Delete(tempPyFile);
			}
		}

		/// <summary>
		/// Runs a shell script in the virtual environment.
		/// </summary>
		/// <param name="shellScript">The shell script to run.</param>
		/// <returns>The result of the script execution contained in a tuple with the standard output and standard error. The standard error is null if there is no error.</returns>
		public async Task<ProcessExecutionResult> RunVenv(string shellScript, string workDir, string? venvActivatePath,
			CancellationToken cancellationToken = default)
		{
			string cmd;
			if (!string.IsNullOrWhiteSpace(venvActivatePath))
				cmd = $"call \"{venvActivatePath}\" && {shellScript}";
			else
				cmd = shellScript;
			return await ShellExecutor.ExecuteWindowsAsync(cmd, workDir: workDir, cancellationToken);
		}
	}
}