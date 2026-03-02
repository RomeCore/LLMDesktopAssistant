using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;
using Python.Runtime;

namespace LLMDesktopAssistant.Modules.Instances
{
	// TODO: Include this later
	// [Module]
	public class PythonModule : Module
	{
		public override void Initialize()
		{
			Directory.CreateDirectory("python");
			Directory.CreateDirectory("python/embed");
			Directory.CreateDirectory("python/venv");

			var pythonDlls = Directory.GetFiles("python/embed", "python*.dll");
			if (pythonDlls.Length == 0)
				throw new DllNotFoundException("Python DLLs not found. Please ensure that the 'python/embed' directory contains the necessary Python DLLs.");
			Runtime.PythonDLL = pythonDlls[0];

			var pathToVirtualEnv = Path.GetFullPath("python/venv");
			var path = Environment.GetEnvironmentVariable("PATH")?.TrimEnd(';');
			path = string.IsNullOrEmpty(path) ? pathToVirtualEnv : path + ";" + pathToVirtualEnv;
			Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
			Environment.SetEnvironmentVariable("PYTHONHOME", pathToVirtualEnv, EnvironmentVariableTarget.Process);
			Environment.SetEnvironmentVariable("PYTHONPATH", $"{pathToVirtualEnv}\\Lib\\site-packages;{pathToVirtualEnv}\\Lib", EnvironmentVariableTarget.Process);

			PythonEngine.PythonHome = pathToVirtualEnv;
			PythonEngine.PythonPath = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process) ?? string.Empty;

			PythonEngine.Initialize(setSysArgv: false, initSigs: false);
		}
	}
}