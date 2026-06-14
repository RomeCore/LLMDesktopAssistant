using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for operating system information: <c>os.*</c>.
	/// Provides platform info, environment, and sleep.
	/// </summary>
	[LuaApi]
	public class LuaApiOs : LuaApiBase
	{
		public override string? Namespace => "os";

		public override string? Manuals => """
			--- os — operating system API

			Provides information about the system and basic OS operations.

			FUNCTIONS:

			--- os.platform()
			  Returns the current platform name.
			  Returns: string — "win", "linux", "osx", "freebsd", or "unknown"

			--- os.arch()
			  Returns the processor architecture.
			  Returns: string — e.g. "x64", "arm64", "x86", "arm"

			--- os.env(name)
			  Returns the value of an environment variable, or nil if not set.
			  Parameters:
			    - name: string — environment variable name (case-insensitive on Windows)
			  Returns: string or nil

			--- os.username()
			  Returns the current user's login name.
			  Returns: string

			--- os.hostname()
			  Returns the machine's host name.
			  Returns: string

			--- os.sleep(seconds)
			  Pauses execution for the specified duration.
			  Parameters:
			    - seconds: number — duration in seconds (can be fractional, e.g. 0.5)
			  Returns: nil

			--- os.pid()
			  Returns the current process ID.
			  Returns: number

			--- os.uptime()
			  Returns the system uptime in seconds.
			  Returns: number

			EXAMPLES:

			  print("Platform:", os.platform())
			  print("Arch:", os.arch())
			  print("User:", os.username())
			  print("Host:", os.hostname())
			  print("PID:", os.pid())
			  print("Uptime:", os.uptime(), "seconds")
			  local path = os.env("PATH")
			  os.sleep(0.5) -- pause 500ms
			""";

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["platform"] = DynValue.NewCallback(new CallbackFunction(Platform));
			ns["arch"] = DynValue.NewCallback(new CallbackFunction(Arch));
			ns["env"] = DynValue.NewCallback(new CallbackFunction(Env));
			ns["username"] = DynValue.NewCallback(new CallbackFunction(Username));
			ns["hostname"] = DynValue.NewCallback(new CallbackFunction(Hostname));
			ns["sleep"] = DynValue.NewCallback(new CallbackFunction(Sleep));
			ns["pid"] = DynValue.NewCallback(new CallbackFunction(Pid));
			ns["uptime"] = DynValue.NewCallback(new CallbackFunction(Uptime));
		}

		private static DynValue Platform(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return DynValue.NewString("win");
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				return DynValue.NewString("linux");
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				return DynValue.NewString("osx");
			if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
				return DynValue.NewString("freebsd");
			return DynValue.NewString("unknown");
		}

		private static DynValue Arch(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var arch = RuntimeInformation.ProcessArchitecture switch
			{
				System.Runtime.InteropServices.Architecture.X64 => "x64",
				System.Runtime.InteropServices.Architecture.X86 => "x86",
				System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
				System.Runtime.InteropServices.Architecture.Arm => "arm",
				_ => "unknown"
			};
			return DynValue.NewString(arch);
		}

		private static DynValue Env(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("os.env(name): at least 1 argument expected.");
			var name = args[0].CastToString();
			if (name == null)
				throw new ScriptRuntimeException("os.env(): first argument must be a string.");
			var value = Environment.GetEnvironmentVariable(name);
			if (value == null)
				return DynValue.Nil;
			return DynValue.NewString(value);
		}

		private static DynValue Username(ScriptExecutionContext ctx, CallbackArguments args)
		{
			return DynValue.NewString(Environment.UserName);
		}

		private static DynValue Hostname(ScriptExecutionContext ctx, CallbackArguments args)
		{
			return DynValue.NewString(Environment.MachineName);
		}

		private static DynValue Sleep(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("os.sleep(seconds): at least 1 argument expected.");
			var seconds = args[0].CastToNumber();
			if (seconds == null)
				throw new ScriptRuntimeException("os.sleep(): first argument must be a number.");
			System.Threading.Thread.Sleep(TimeSpan.FromSeconds(seconds.Value));
			return DynValue.Nil;
		}

		private static DynValue Pid(ScriptExecutionContext ctx, CallbackArguments args)
		{
			return DynValue.NewNumber(Environment.ProcessId);
		}

		private static DynValue Uptime(ScriptExecutionContext ctx, CallbackArguments args)
		{
			return DynValue.NewNumber(Environment.TickCount64 / 1000.0);
		}
	}
}
