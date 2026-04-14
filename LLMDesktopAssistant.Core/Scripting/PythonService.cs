using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
		/// Gets the path to the virtual environment directory.
		/// </summary>
		public string VenvPath { get; }

		/// <summary>
		/// Gets the path to the 'activate' script for the virtual environment.
		/// </summary>
		public string ActivateVenvPath { get; }

		public PythonService()
		{
			VenvPath = Path.GetFullPath("python/venv");
			ActivateVenvPath = Path.Combine(VenvPath, "Scripts", "activate.bat");

			if (!File.Exists(ActivateVenvPath))
				throw new FileNotFoundException($"{Path.GetFileName(ActivateVenvPath)} not found", ActivateVenvPath);

			Directory.CreateDirectory(VenvPath);
		}

		/// <summary>
		/// Executes a Python script.
		/// </summary>
		/// <param name="script">The Python script to execute.</param>
		/// <returns>The result of the script execution.</returns>
		public async Task<ProcessExecutionResult> RunScript(string script, string? workDir = null,
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
				// cmd /c "call activate.bat && <command>"
				var cmd = $"call \"{ActivateVenvPath}\" && python \"{tempPyFile}\"";
				return await ShellExecutor.ExecuteWindowsAsync(cmd, workDir: workDir ?? VenvPath, cancellationToken);
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
		public async Task<ProcessExecutionResult> RunVenv(string shellScript, string? workDir = null, CancellationToken cancellationToken = default)
		{
			// cmd /c "call activate.bat && <command>"
			var cmd = $"call \"{ActivateVenvPath}\" && {shellScript}";
			return await ShellExecutor.ExecuteWindowsAsync(cmd, workDir: workDir ?? VenvPath, cancellationToken);
		}
	}
}