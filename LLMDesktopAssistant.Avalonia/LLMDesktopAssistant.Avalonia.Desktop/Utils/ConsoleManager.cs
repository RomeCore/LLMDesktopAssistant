using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Avalonia.Desktop.Utils
{
	public static class ConsoleManager
	{
		private static bool _hasAllocatedConsole = false;
		private static bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		private delegate void AllocConsoleDelegate();
		private delegate void FreeConsoleDelegate();

		private static readonly AllocConsoleDelegate? _allocConsole;
		private static readonly FreeConsoleDelegate? _freeConsole;

		static ConsoleManager()
		{
			if (_isWindows)
			{
				try
				{
					IntPtr kernel32 = NativeLibrary.Load("kernel32.dll");
					IntPtr allocPtr = NativeLibrary.GetExport(kernel32, "AllocConsole");
					IntPtr freePtr = NativeLibrary.GetExport(kernel32, "FreeConsole");

					_allocConsole = Marshal.GetDelegateForFunctionPointer<AllocConsoleDelegate>(allocPtr);
					_freeConsole = Marshal.GetDelegateForFunctionPointer<FreeConsoleDelegate>(freePtr);
				}
				catch
				{
					_isWindows = false;
				}
			}
		}

		public static void EnsureConsole()
		{
			if (_hasAllocatedConsole) return;

			if (_isWindows && _allocConsole != null)
			{
				_allocConsole();
				_hasAllocatedConsole = true;
			}
			else
			{
				Console.WriteLine("Console management not supported on this platform");
				_hasAllocatedConsole = true;
			}
		}

		public static void ReleaseConsole()
		{
			if (!_hasAllocatedConsole) return;

			if (_isWindows && _freeConsole != null)
			{
				_freeConsole();
				_hasAllocatedConsole = false;
			}
		}
	}
}