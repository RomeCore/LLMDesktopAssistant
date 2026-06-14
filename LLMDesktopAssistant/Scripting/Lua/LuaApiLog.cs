using System;
using System.Collections.Generic;
using System.Text;
using MoonSharp.Interpreter;
using Serilog;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for structured logging via Serilog: <c>dass.log.*</c>.
	/// Supports all Serilog levels with template-based message formatting.
	/// </summary>
	[LuaApi]
	public class LuaApiLog : LuaApiBase
	{
		private readonly ILogger _logger;

		public override string? Namespace => "dass.log";

		public override string? Manuals => """
			--- dass.log — structured logging API

			Logs messages at various levels with template-based formatting.
			Supports named placeholders like {Name}, {Count}, etc.

			FUNCTIONS:

			--- dass.log.verbose(template, ...)
			--- dass.log.debug(template, ...)
			--- dass.log.info(template, ...)
			--- dass.log.warning(template, ...)
			--- dass.log.error(template, ...)
			--- dass.log.fatal(template, ...)

			  Each function takes a message template string followed by
			  optional positional arguments to fill in placeholders.

			  Parameters:
			    - template: string — message template (e.g. "User {Name} logged in")
			    - ...: any (optional) — values for placeholders, in order

			  Returns: nil

			EXAMPLES:

			  dass.log.info("Hello {Name}, you are {Age} years old", "John", 30)
			  dass.log.warning("Disk space low: {FreeMB} MB remaining", 42)
			  dass.log.error("Failed to process file {Path}: {Error}", "/tmp/x.txt", "not found")
			  dass.log.debug("X = {X}, Y = {Y}", 10, 20)
			  dass.log.verbose("Entering function {Func}", "calculate")
			  dass.log.fatal("System halted: {Reason}", "kernel panic")
			""";

		public LuaApiLog(ILogger logger)
		{
			_logger = logger;
		}

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["verbose"] = DynValue.NewCallback(new CallbackFunction(LogVerbose));
			ns["debug"] = DynValue.NewCallback(new CallbackFunction(LogDebug));
			ns["info"] = DynValue.NewCallback(new CallbackFunction(LogInfo));
			ns["warning"] = DynValue.NewCallback(new CallbackFunction(LogWarning));
			ns["error"] = DynValue.NewCallback(new CallbackFunction(LogError));
			ns["fatal"] = DynValue.NewCallback(new CallbackFunction(LogFatal));
		}

		private DynValue LogVerbose(ScriptExecutionContext ctx, CallbackArguments args)
		{
			LogWithLevel(args, (t, p) => _logger.Verbose(t, p));
			return DynValue.Nil;
		}

		private DynValue LogDebug(ScriptExecutionContext ctx, CallbackArguments args)
		{
			LogWithLevel(args, (t, p) => _logger.Debug(t, p));
			return DynValue.Nil;
		}

		private DynValue LogInfo(ScriptExecutionContext ctx, CallbackArguments args)
		{
			LogWithLevel(args, (t, p) => _logger.Information(t, p));
			return DynValue.Nil;
		}

		private DynValue LogWarning(ScriptExecutionContext ctx, CallbackArguments args)
		{
			LogWithLevel(args, (t, p) => _logger.Warning(t, p));
			return DynValue.Nil;
		}

		private DynValue LogError(ScriptExecutionContext ctx, CallbackArguments args)
		{
			LogWithLevel(args, (t, p) => _logger.Error(t, p));
			return DynValue.Nil;
		}

		private DynValue LogFatal(ScriptExecutionContext ctx, CallbackArguments args)
		{
			LogWithLevel(args, (t, p) => _logger.Fatal(t, p));
			return DynValue.Nil;
		}

		private static void LogWithLevel(CallbackArguments args, Action<string, object[]> logAction)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("dass.log.LEVEL(template, ...): at least 1 argument expected (template).");

			var template = args[0].CastToString();
			if (template == null)
				throw new ScriptRuntimeException("dass.log.LEVEL(): first argument must be a string (template).");

			if (args.Count == 1)
			{
				logAction(template, []);
				return;
			}

			var propertyValues = new object[args.Count - 1];
			for (int i = 1; i < args.Count; i++)
			{
				propertyValues[i - 1] = DynValueToObject(args[i]);
			}

			logAction(template, propertyValues);
		}

		private static object DynValueToObject(DynValue val)
		{
			switch (val.Type)
			{
				case DataType.Nil:
					return null!;
				case DataType.Boolean:
					return val.Boolean;
				case DataType.Number:
					return val.Number;
				case DataType.String:
					return val.String;
				case DataType.Table:
					// Convert to simple dict/array for readability
					var t = val.Table;
					var dict = new Dictionary<string, object?>();
					foreach (var kv in t.Pairs)
					{
						dict.Add(kv.Key.ToPrintString(), DynValueToObject(kv.Value));
					}
					return dict;
				default:
					return val.ToPrintString();
			}
		}
	}
}
