using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.Utils
{
	public static class ConsoleManager
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		private static extern void AllocConsole();
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		private static extern void FreeConsole();

		private static bool _hasAllocatedConsole = false;

		/// <summary>
		/// Allocates a new console if one is not already allocated.
		/// </summary>
		public static void EnsureConsole()
		{
			if (!_hasAllocatedConsole)
			{
				_hasAllocatedConsole = true;
				AllocConsole();
			}
		}

		/// <summary>
		/// Releases the console window if it was previously allocated.
		/// </summary>
		public static void ReleaseConsole()
		{
			if (_hasAllocatedConsole)
			{
				_hasAllocatedConsole = false;
				FreeConsole();
			}
		}
	}
}