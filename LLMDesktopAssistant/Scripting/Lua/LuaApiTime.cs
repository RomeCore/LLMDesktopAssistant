using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/*

	/// <summary>
	/// Lua API for time operations: <c>time.*</c>.
	/// Provides stopwatch, duration formatting, unit conversion,
	/// high-resolution timestamps, delays, timers, and execution measurement.
	/// </summary>
	[LuaApi(chatScoped: false)]
	public class LuaApiTime : LuaApiBase
	{
		public override string? Namespace => "time";

		public override string? Manuals => """
			--- time — time measurement and formatting API

			Provides stopwatch, duration formatting, unit conversion,
			high-resolution timestamps, delays, timers, and execution measurement.

			FUNCTIONS:

			--- time.now()
			  Returns the current Unix timestamp in seconds (with millisecond precision as float).
			  Returns: number — e.g. 1712345678.123

			--- time.now_ms()
			  Returns the current Unix timestamp in milliseconds.
			  Returns: number — integer, e.g. 1712345678123

			--- time.now_ns()
			  Returns a high-resolution monotonic timestamp in nanoseconds.
			  Does NOT represent wall-clock time; only suitable for computing differences.
			  Returns: number — nanoseconds since an undefined epoch

			--- time.sleep(ms)
			  Pauses execution for the specified number of milliseconds.
			  Parameters:
			    - ms: number — duration in milliseconds (can be fractional)
			  Returns: nil

			--- time.sleep_until(timestamp)
			  Pauses execution until the specified Unix timestamp (seconds).
			  Parameters:
			    - timestamp: number — Unix timestamp in seconds
			  Returns: nil

			--- time.format(ms, [style])
			  Formats a duration in milliseconds into a human-readable string.
			  Parameters:
			    - ms: number — duration in milliseconds
			    - style: string (optional) — "auto" (default), "full", "short", "clock"
			  Returns: string

			--- time.convert(value, from, to)
			  Converts a time value from one unit to another.
			  Supported units: ns, us, μs, ms, s, m, h, d, wk
			  Parameters:
			    - value: number — the value to convert
			    - from: string — source unit
			    - to: string — target unit
			  Returns: number

			--- time.measure(func)
			  Executes a function and measures its execution time.
			  Parameters:
			    - func: function — the function to execute
			  Returns: duration_ms (number), ... (function return values)

			--- time.stopwatch()
			  Creates a new stopwatch object for measuring elapsed time.
			  Returns: stopwatch userdata with :elapsed(), :elapsed_ns(),
			            :reset(), and :to_string() methods.
			""";

		/* REMOVED MANUALS
		
			--- time.with_timeout(func, timeout_ms)
			  Executes a function in a snapshot runtime with a timeout.
			  If the function exceeds the timeout, it is abandoned.
			  Parameters:
			    - func: function — the function to execute
			    - timeout_ms: number — timeout in milliseconds
			  Returns:
			    - ok: boolean — true if completed within timeout
			    - elapsed_ms: number — actual execution time
			    - result or error: any — return value or error message
		
			--- time.set_timeout(ms, callback)
			  Schedules a callback to run once after the specified delay.
			  The callback runs in an isolated snapshot runtime.
			  Returns: timer userdata with :cancel() and :is_pending() methods.
			  Example:
			    local t = time.set_timeout(1000, function()
			      print("Hello 1 second later!")
			    end)
			    t:cancel()  -- cancel if needed

			--- time.set_interval(ms, callback)
			  Schedules a callback to run repeatedly every `ms` milliseconds.
			  Each invocation runs in an isolated snapshot runtime.
			  Returns: timer userdata with :cancel() and :is_pending() methods.
			  Example:
			    local t = time.set_interval(500, function()
			      print("tick")
			    end)
			    t:cancel()  -- stop the interval

		

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["now"] = DynValue.NewCallback(Now);
			ns["now_ms"] = DynValue.NewCallback(NowMs);
			ns["now_ns"] = DynValue.NewCallback(NowNs);
			ns["sleep"] = DynValue.NewCallback(Sleep);
			ns["sleep_until"] = DynValue.NewCallback(SleepUntil);
			ns["format"] = DynValue.NewCallback(Format);
			ns["convert"] = DynValue.NewCallback(Convert);
			ns["measure"] = DynValue.NewCallback(Measure);
			ns["stopwatch"] = DynValue.NewCallback(NewStopwatch);
			// These are broken (thanks to moonsharp)
			// ns["with_timeout"] = DynValue.NewCallback(WithTimeout);
			// ns["set_timeout"] = DynValue.NewCallback(SetTimeout);
			// ns["set_interval"] = DynValue.NewCallback(SetInterval);
		}

		private static DynValue Now(ScriptExecutionContext ctx, CallbackArguments args)
		{
			return DynValue.NewNumber(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0);
		}

		private static DynValue NowMs(ScriptExecutionContext ctx, CallbackArguments args)
		{
			return DynValue.NewNumber(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
		}

		private static DynValue NowNs(ScriptExecutionContext ctx, CallbackArguments args)
		{
			return DynValue.NewNumber(Stopwatch.GetTimestamp() * 1_000_000_000.0 / Stopwatch.Frequency);
		}

		private static DynValue Sleep(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("time.sleep(ms): at least 1 argument expected.");
			var ms = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("time.sleep(): argument must be a number (milliseconds).");

			if (ms > 0)
			{
				var delayMs = (int)Math.Round(ms);
				if (delayMs > 0)
					Thread.Sleep(delayMs);
				else
					Thread.SpinWait(1);
			}
			return DynValue.Nil;
		}

		private static DynValue SleepUntil(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("time.sleep_until(timestamp): at least 1 argument expected.");
			var ts = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("time.sleep_until(): argument must be a number (Unix timestamp).");

			var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
			var diff = ts - now;
			if (diff > 0)
			{
				var delayMs = (int)Math.Round(diff * 1000);
				if (delayMs > 0)
					Thread.Sleep(delayMs);
			}
			return DynValue.Nil;
		}

		private static double ToNanoseconds(double value, string unit) => unit switch
		{
			"ns" => value,
			"us" or "μs" => value * 1000,
			"ms" => value * 1_000_000,
			"s" or "sec" or "seconds" => value * 1_000_000_000,
			"m" or "min" or "minutes" => value * 60_000_000_000,
			"h" or "hours" => value * 3_600_000_000_000,
			"d" or "days" => value * 86_400_000_000_000,
			"wk" or "weeks" => value * 604_800_000_000_000,
			_ => throw new ScriptRuntimeException($"time.convert(): unknown unit '{unit}'")
		};

		private static double FromNanoseconds(double ns, string unit) => unit switch
		{
			"ns" => ns,
			"us" or "μs" => ns / 1000,
			"ms" => ns / 1_000_000,
			"s" or "sec" or "seconds" => ns / 1_000_000_000,
			"m" or "min" or "minutes" => ns / 60_000_000_000,
			"h" or "hours" => ns / 3_600_000_000_000,
			"d" or "days" => ns / 86_400_000_000_000,
			"wk" or "weeks" => ns / 604_800_000_000_000,
			_ => throw new ScriptRuntimeException($"time.convert(): unknown unit '{unit}'")
		};

		private static DynValue Convert(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 3)
				throw new ScriptRuntimeException("time.convert(value, from, to): at least 3 arguments expected.");
			var value = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("time.convert(): first argument must be a number.");
			var from = args[1].CastToString()
				?? throw new ScriptRuntimeException("time.convert(): second argument must be a string (source unit).");
			var to = args[2].CastToString()
				?? throw new ScriptRuntimeException("time.convert(): third argument must be a string (target unit).");

			return DynValue.NewNumber(FromNanoseconds(ToNanoseconds(value, from.ToLowerInvariant()), to.ToLowerInvariant()));
		}

		private static DynValue Format(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("time.format(ms, [style]): at least 1 argument expected.");
			var ms = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("time.format(): first argument must be a number (milliseconds).");

			var style = "auto";
			if (args.Count > 1 && !args[1].IsNil())
			{
				var s = args[1].CastToString();
				if (s != null) style = s.ToLowerInvariant();
			}

			return DynValue.NewString(FormatDuration(ms, style));
		}

		private static string FormatDuration(double ms, string style)
		{
			if (ms < 0) ms = 0;
			return style switch
			{
				"auto" => FormatAuto(ms),
				"full" => FormatFull(ms),
				"short" => FormatShort(ms),
				"clock" or "timestamp" => FormatClock(ms),
				_ => FormatAuto(ms)
			};
		}

		private static string FormatAuto(double ms)
		{
			if (ms < 0.001) return $"{(ms * 1_000_000):F0}ns";
			if (ms < 1) return $"{(ms * 1000):F0}μs";
			if (ms < 1000) return $"{ms:F1}ms";
			if (ms < 60_000) return $"{ms / 1000:F2}s";

			double seconds = ms / 1000;
			double minutes = seconds / 60;
			double hours = minutes / 60;
			double days = hours / 24;

			if (days >= 1)
			{
				var wholeDays = (int)days;
				var remainingHours = hours - wholeDays * 24;
				return remainingHours >= 1 ? $"{wholeDays}d {(int)remainingHours}h" : $"{wholeDays}d";
			}
			if (hours >= 1)
			{
				var wholeHours = (int)hours;
				var remainingMinutes = minutes - wholeHours * 60;
				return remainingMinutes >= 1 ? $"{wholeHours}h {(int)remainingMinutes}m" : $"{wholeHours}h";
			}
			var wholeMinutes = (int)minutes;
			var remainingSeconds = seconds - wholeMinutes * 60;
			return remainingSeconds >= 1 ? $"{wholeMinutes}m {(int)remainingSeconds}s" : $"{wholeMinutes}m";
		}

		private static string FormatFull(double ms)
		{
			if (ms < 0.001) return $"{(ms * 1_000_000):F0} nanoseconds";
			if (ms < 1) return $"{(ms * 1000):F2} microseconds";

			var parts = new List<string>();
			var totalSeconds = ms / 1000;

			var weeks = (int)(totalSeconds / (7 * 86400));
			totalSeconds -= weeks * 7 * 86400;
			if (weeks > 0) parts.Add($"{weeks} week{(weeks != 1 ? "s" : "")}");

			var days = (int)(totalSeconds / 86400);
			totalSeconds -= days * 86400;
			if (days > 0) parts.Add($"{days} day{(days != 1 ? "s" : "")}");

			var hours = (int)(totalSeconds / 3600);
			totalSeconds -= hours * 3600;
			if (hours > 0) parts.Add($"{hours} hour{(hours != 1 ? "s" : "")}");

			var minutes = (int)(totalSeconds / 60);
			totalSeconds -= minutes * 60;
			if (minutes > 0) parts.Add($"{minutes} minute{(minutes != 1 ? "s" : "")}");

			if (totalSeconds >= 1 || parts.Count == 0)
				parts.Add($"{totalSeconds:F3} seconds");
			else
			{
				var remainingMs = totalSeconds * 1000;
				if (remainingMs >= 1)
					parts.Add($"{remainingMs:F0} ms");
				else if (remainingMs > 0)
					parts.Add($"{(remainingMs * 1000):F0} μs");
			}

			return string.Join(", ", parts);
		}

		private static string FormatShort(double ms)
		{
			if (ms < 0.001) return $"{(ms * 1_000_000):F0}ns";
			if (ms < 1) return $"{(ms * 1000):F0}μs";

			var totalSeconds = ms / 1000;
			var parts = new List<string>();

			var days = (int)(totalSeconds / 86400);
			totalSeconds -= days * 86400;
			if (days > 0) parts.Add($"{days}d");

			var hours = (int)(totalSeconds / 3600);
			totalSeconds -= hours * 3600;
			if (hours > 0) parts.Add($"{hours}h");

			var minutes = (int)(totalSeconds / 60);
			totalSeconds -= minutes * 60;
			if (minutes > 0) parts.Add($"{minutes}m");

			if (totalSeconds >= 1 || parts.Count == 0)
				parts.Add($"{totalSeconds:F3}s");
			else
			{
				var remainingMs = totalSeconds * 1000;
				parts.Add(remainingMs >= 1 ? $"{remainingMs:F0}ms" : $"{(remainingMs * 1000):F0}μs");
			}

			return string.Join(" ", parts);
		}

		private static string FormatClock(double ms)
		{
			if (ms < 0) ms = 0;
			var totalSeconds = (long)(ms / 1000);
			var remainingMs = (int)(ms % 1000);
			return $"{totalSeconds / 3600:D2}:{(totalSeconds % 3600) / 60:D2}:{totalSeconds % 60:D2}.{remainingMs:D3}";
		}

		private static DynValue Measure(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1 || args[0].Type != DataType.Function)
				throw new ScriptRuntimeException("time.measure(func): first argument must be a function.");

			var func = args[0];
			var script = ctx.GetScript();
			var sw = Stopwatch.StartNew();

			DynValue result;
			try { result = script.Call(func); }
			catch (Exception ex)
			{
				sw.Stop();
				throw new ScriptRuntimeException($"time.measure(): failed after {sw.Elapsed.TotalMilliseconds:F1}ms: {ex.Message}");
			}

			sw.Stop();
			var elapsedMs = sw.Elapsed.TotalMilliseconds;

			// Return elapsed_ms as first value, then unpack the original function's returns
			if (result.Type == DataType.Tuple)
			{
				var tuple = result.Tuple;
				var returns = new DynValue[tuple.Length + 1];
				returns[0] = DynValue.NewNumber(elapsedMs);
				Array.Copy(tuple, 0, returns, 1, tuple.Length);
				return DynValue.NewTuple(returns);
			}
			if (result.Type != DataType.Nil && result.Type != DataType.Void)
				return DynValue.NewTuple(DynValue.NewNumber(elapsedMs), result);

			return DynValue.NewNumber(elapsedMs);
		}

		private static DynValue WithTimeout(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("time.with_timeout(func, timeout_ms): at least 2 arguments expected.");
			if (args[0].Type != DataType.Function)
				throw new ScriptRuntimeException("time.with_timeout(): first argument must be a function.");
			var timeoutMs = args[1].CastToNumber()
				?? throw new ScriptRuntimeException("time.with_timeout(): second argument must be a positive number (timeout in ms).");

			var func = args[0];
			var script = ctx.GetScript();

			var sw = Stopwatch.StartNew();
			DynValue? result = null;
			Exception? error = null;
			var cts = new CancellationTokenSource();

			var task = Task.Run(() =>
			{
				try { result = script.Call(func.Function); }
				catch (Exception ex) { error = ex; }
			}, cts.Token);

			var finished = task.Wait((int)Math.Round(timeoutMs));
			sw.Stop();
			var elapsedMs = sw.Elapsed.TotalMilliseconds;

			if (finished)
			{
				if (error != null)
					return DynValue.NewTuple(
						DynValue.NewBoolean(false),
						DynValue.NewNumber(elapsedMs),
						DynValue.NewString(error.Message));

				if (result!.Type == DataType.Tuple)
				{
					var tuple = result.Tuple;
					var returns = new DynValue[tuple.Length + 2];
					returns[0] = DynValue.NewBoolean(true);
					returns[1] = DynValue.NewNumber(elapsedMs);
					Array.Copy(tuple, 0, returns, 2, tuple.Length);
					return DynValue.NewTuple(returns);
				}

				if (result.Type != DataType.Nil && result.Type != DataType.Void)
					return DynValue.NewTuple(
						DynValue.NewBoolean(true),
						DynValue.NewNumber(elapsedMs),
						result);

				return DynValue.NewTuple(
					DynValue.NewBoolean(true),
					DynValue.NewNumber(elapsedMs));
			}
			else
			{
				cts.Cancel(); // Abandon the background task
				return DynValue.NewTuple(
					DynValue.NewBoolean(false),
					DynValue.NewNumber(elapsedMs),
					DynValue.NewString($"Timeout exceeded: {FormatAuto(elapsedMs)} (limit was {FormatAuto(timeoutMs)})"));
			}
		}

		private static DynValue SetTimeout(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("time.set_timeout(ms, callback): at least 2 arguments expected.");
			if (args[0].Type != DataType.Number)
				throw new ScriptRuntimeException("time.set_timeout(): first argument must be a number (milliseconds).");
			if (args[1].Type != DataType.Function)
				throw new ScriptRuntimeException("time.set_timeout(): second argument must be a function (callback).");

			return UserData.Create(new LuaTimer(args[0].Number, args[1], ctx.GetScript(), isRepeating: false));
		}

		private static DynValue SetInterval(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("time.set_interval(ms, callback): at least 2 arguments expected.");
			if (args[0].Type != DataType.Number)
				throw new ScriptRuntimeException("time.set_interval(): first argument must be a number (milliseconds).");
			if (args[1].Type != DataType.Function)
				throw new ScriptRuntimeException("time.set_interval(): second argument must be a function (callback).");

			return UserData.Create(new LuaTimer(args[0].Number, args[1], ctx.GetScript(), isRepeating: true));
		}

		private static DynValue NewStopwatch(ScriptExecutionContext ctx, CallbackArguments args)
		{
			return UserData.Create(new LuaStopwatch());
		}
	}

	*/
}
