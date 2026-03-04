using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;
using Serilog;

namespace LLMDesktopAssistant.Scripting
{
	[Module(Order = int.MinValue)]
	public class PythonModule : Module
	{
		/// <summary>
		/// Gets the path to the virtual environment directory.
		/// </summary>
		public string PythonVenvPath { get; }

		/// <summary>
		/// Gets the path to the temporary script files.
		/// </summary>
		public string PythonTempScriptPath { get; }

		public PythonModule()
		{
			PythonVenvPath = Path.GetFullPath("python/venv");
			PythonTempScriptPath = Path.GetFullPath("python/temp");

			Directory.CreateDirectory(PythonVenvPath);
			Directory.CreateDirectory(PythonTempScriptPath);
		}

		/// <summary>
		/// Executes a Python script.
		/// </summary>
		/// <param name="script">The Python script to execute.</param>
		/// <returns>The result of the script execution.</returns>
		public async Task<(string stdOut, string? stdErr)> RunScript(string script)
		{
			var venvPath = PythonVenvPath;
			var activateBat = Path.Combine(venvPath, "Scripts", "activate.bat");

			if (!File.Exists(activateBat))
				throw new FileNotFoundException("activate.bat not found", activateBat);

			script = $"""
				import sys
				sys.stdout.reconfigure(encoding="utf-8")
				sys.stderr.reconfigure(encoding="utf-8")
				
				{script}
				""";

			var tempPyFile = Path.GetFullPath(Path.Combine(PythonTempScriptPath, $"{Guid.NewGuid()}.py"));
			File.WriteAllText(tempPyFile, script);

			// cmd /c "call activate.bat && <command>"
			var cmd = $"/c \"call \"{activateBat}\" && python \"{tempPyFile}\"\"";

			var psi = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = cmd,
				WorkingDirectory = venvPath,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var process = new Process { StartInfo = psi };

			process.Start();

			var stdoutTask = process.StandardOutput.ReadToEndAsync();
			var stderrTask = process.StandardError.ReadToEndAsync();

			await Task.WhenAll(stdoutTask, stderrTask);
			await process.WaitForExitAsync();

			File.Delete(tempPyFile);

			if (process.ExitCode != 0)
			{
				Log.Error(
					"Python script failed (exit {code})\nSTDOUT:\n{out}\nSTDERR:\n{err}",
					process.ExitCode,
					stdoutTask.Result,
					stderrTask.Result
				);

				return (stdoutTask.Result, stderrTask.Result);
			}

			Log.Information(
				"Python script completed successfully\n{out}",
				stdoutTask.Result
			);

			return (stdoutTask.Result, null);
		}

		/// <summary>
		/// Runs a shell script in the virtual environment.
		/// </summary>
		/// <param name="shellScript">The shell script to run.</param>
		/// <returns>The result of the script execution contained in a tuple with the standard output and standard error. The standard error is null if there is no error.</returns>
		public async Task<(string stdOut, string? stdErr)> RunVenv(string shellScript)
		{
			var venvPath = PythonVenvPath;
			var activateBat = Path.Combine(venvPath, "Scripts", "activate.bat");

			if (!File.Exists(activateBat))
				throw new FileNotFoundException("activate.bat not found", activateBat);

			// cmd /c "call activate.bat && <command>"
			var cmd = $"/c \"call \"{activateBat}\" && {shellScript}\"";

			var psi = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = cmd,
				WorkingDirectory = venvPath,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var process = new Process { StartInfo = psi };

			process.Start();

			var stdoutTask = process.StandardOutput.ReadToEndAsync();
			var stderrTask = process.StandardError.ReadToEndAsync();

			await Task.WhenAll(stdoutTask, stderrTask);
			await process.WaitForExitAsync();

			if (process.ExitCode != 0)
			{
				Log.Error(
					"Venv command failed (exit {code})\nSTDOUT:\n{out}\nSTDERR:\n{err}",
					process.ExitCode,
					stdoutTask.Result,
					stderrTask.Result
				);

				return (stdoutTask.Result, stderrTask.Result);
			}

			Log.Information(
				"Venv command completed successfully\n{out}",
				stdoutTask.Result
			);

			return (stdoutTask.Result, null);
		}
	}
}