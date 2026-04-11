using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.Utils
{
	public static class Directories
	{
		/// <summary>
		/// The path where to store the temporary scripts to be executed and instantly deleted after execution.
		/// </summary>
		public const string TempScripts = "temp/scripts/";

		/// <summary>
		/// The path where to store the settings files. These are usually configuration files that need to persist across sessions.
		/// </summary>
		public const string Settings = "settings/";

		public const string DefaultWorkingDirectory = "working_dir/";

		public static void EnsureAll()
		{
			Directory.CreateDirectory(TempScripts);
			Directory.CreateDirectory(Settings);
			Directory.CreateDirectory(DefaultWorkingDirectory);
		}
	}
}