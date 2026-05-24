using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for date and time operations: <c>datetime.*</c>.
	/// </summary>
	[LuaApi]
	public class LuaApiDateTime : LuaApiBase
	{
		public override string? Namespace => "datetime";

		public override string? Manuals => """
			--- datetime — date and time API

			All functions work with datetime tables:
			  { year, month, day, hour, min, sec, ms, is_dst, utc_offset (minutes), timestamp (unix epoch seconds) }

			FUNCTIONS:

			--- datetime.now()
			  Returns the current UTC time as a datetime table.
			  Returns: table

			--- datetime.now_local()
			  Returns the current local time as a datetime table.
			  Returns: table

			--- datetime.parse(str, [format])
			  Parses a date/time string into a datetime table.
			  Parameters:
			    - str: string — date/time string
			    - format: string (optional) — .NET date format string. 
			      If omitted, ISO 8601 and common formats are auto-detected.
			  Returns: table or nil on failure

			--- datetime.format(dt, [format])
			  Formats a datetime table into a string.
			  Parameters:
			    - dt: table — datetime table
			    - format: string (optional) — .NET date format string.
			      Default: "o" (ISO 8601: "2026-05-25T02:54:00.0000000Z")
			  Returns: string

			--- datetime.diff(dt1, dt2)
			  Returns the difference between two datetime tables in seconds.
			  Parameters:
			    - dt1: table — first datetime
			    - dt2: table — second datetime
			  Returns: number — dt1 - dt2 in seconds

			--- datetime.add(dt, interval)
			  Adds a time interval to a datetime table.
			  Parameters:
			    - dt: table — datetime table
			    - interval: table — interval with fields:
			      - years: number (optional)
			      - months: number (optional)
			      - days: number (optional)
			      - hours: number (optional)
			      - minutes: number (optional)
			      - seconds: number (optional)
			      - ms: number (optional, milliseconds)
			  Returns: table — new datetime (original unchanged)

			--- datetime.utc(dt)
			  Converts a datetime table to UTC.
			  Parameters:
			    - dt: table — datetime table
			  Returns: table

			--- datetime.timestamp(dt)
			  Returns the Unix epoch seconds from a datetime table.
			  Parameters:
			    - dt: table — datetime table
			  Returns: number

			--- datetime.from_timestamp(ts)
			  Creates a datetime table from Unix epoch seconds.
			  Parameters:
			    - ts: number — Unix epoch seconds
			  Returns: table

			EXAMPLES:

			  local now = datetime.now()
			  print(now.year, now.month, now.day)

			  -- Format
			  print(datetime.format(now, "yyyy-MM-dd HH:mm:ss"))

			  -- Parse
			  local dt = datetime.parse("2026-05-25T03:00:00Z")

			  -- Difference
			  local d1 = datetime.now()
			  local d2 = datetime.add(d1, {hours = 1})
			  print(datetime.diff(d2, d1)) -- 3600

			  -- Add
			  local tomorrow = datetime.add(now, {days = 1})

			  -- Timestamp
			  print(datetime.timestamp(now))
			""";

		public override void Populate(Table globals, Table ns)
		{
			ns["now"] = DynValue.NewCallback(new CallbackFunction(Now));
			ns["now_local"] = DynValue.NewCallback(new CallbackFunction(Local));
			ns["parse"] = DynValue.NewCallback(new CallbackFunction(Parse));
			ns["format"] = DynValue.NewCallback(new CallbackFunction(Format));
			ns["diff"] = DynValue.NewCallback(new CallbackFunction(Diff));
			ns["add"] = DynValue.NewCallback(new CallbackFunction(Add));
			ns["utc"] = DynValue.NewCallback(new CallbackFunction(Utc));
			ns["timestamp"] = DynValue.NewCallback(new CallbackFunction(Timestamp));
			ns["from_timestamp"] = DynValue.NewCallback(new CallbackFunction(FromTimestamp));
		}

		private static DynValue Now(ScriptExecutionContext ctx, CallbackArguments args)
		{
			return DynValue.NewTable(DateTimeToTable(ctx.OwnerScript, DateTimeOffset.UtcNow));
		}

		private static DynValue Local(ScriptExecutionContext ctx, CallbackArguments args)
		{
			return DynValue.NewTable(DateTimeToTable(ctx.OwnerScript, DateTimeOffset.Now));
		}

		private static DynValue Parse(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("datetime.parse(str, [format]): at least 1 argument expected.");

			var str = args[0].CastToString();
			if (str == null)
				throw new ScriptRuntimeException("datetime.parse(): first argument must be a string.");

			try
			{
				if (args.Count > 1 && !args[1].IsNil())
				{
					var format = args[1].CastToString();
					if (format == null)
						throw new ScriptRuntimeException("datetime.parse(): second argument must be a string (format).");
					var dt = DateTimeOffset.ParseExact(str, format, CultureInfo.InvariantCulture);
					return DynValue.NewTable(DateTimeToTable(ctx.OwnerScript, dt));
				}

				var parsed = DateTimeOffset.Parse(str, CultureInfo.InvariantCulture);
				return DynValue.NewTable(DateTimeToTable(ctx.OwnerScript, parsed));
			}
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"datetime.parse() error: {ex.Message}");
			}
		}

		private static DynValue Format(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("datetime.format(dt, [format]): at least 1 argument expected.");

			var dt = TableToDateTime(args[0]);
			if (dt == null)
				throw new ScriptRuntimeException("datetime.format(): first argument must be a datetime table.");

			var format = "o"; // ISO 8601 default
			if (args.Count > 1 && !args[1].IsNil())
			{
				var fmt = args[1].CastToString();
				if (fmt == null)
					throw new ScriptRuntimeException("datetime.format(): second argument must be a string (format).");
				format = fmt;
			}

			try
			{
				return DynValue.NewString(dt.Value.ToUniversalTime().ToString(format, CultureInfo.InvariantCulture));
			}
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"datetime.format() error: {ex.Message}");
			}
		}

		private static DynValue Diff(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("datetime.diff(dt1, dt2): at least 2 arguments expected.");

			var dt1 = TableToDateTime(args[0]);
			var dt2 = TableToDateTime(args[1]);
			if (dt1 == null || dt2 == null)
				throw new ScriptRuntimeException("datetime.diff(): both arguments must be datetime tables.");

			return DynValue.NewNumber((dt1.Value - dt2.Value).TotalSeconds);
		}

		private static DynValue Add(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("datetime.add(dt, interval): at least 2 arguments expected.");

			var dt = TableToDateTime(args[0]);
			if (dt == null)
				throw new ScriptRuntimeException("datetime.add(): first argument must be a datetime table.");

			var interval = args[1];
			if (interval.Type != DataType.Table)
				throw new ScriptRuntimeException("datetime.add(): second argument must be a table (interval).");

			var t = interval.Table;
			var years = GetIntField(t, "years", 0);
			var months = GetIntField(t, "months", 0);
			var days = GetIntField(t, "days", 0);
			var hours = GetIntField(t, "hours", 0);
			var minutes = GetIntField(t, "minutes", 0);
			var seconds = GetIntField(t, "seconds", 0);
			var ms = GetIntField(t, "ms", 0);

			var result = dt.Value.ToUniversalTime();
			if (years != 0 || months != 0)
			{
				// Add years/months via calendar (preserving time)
				var utc = result.DateTime;
				var newDt = utc.AddYears(years).AddMonths(months);
				result = new DateTimeOffset(newDt, TimeSpan.Zero);
			}
			result = result.AddDays(days).AddHours(hours).AddMinutes(minutes).AddSeconds(seconds).AddMilliseconds(ms);

			return DynValue.NewTable(DateTimeToTable(ctx.OwnerScript, result));
		}

		private static DynValue Utc(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("datetime.utc(dt): at least 1 argument expected.");

			var dt = TableToDateTime(args[0]);
			if (dt == null)
				throw new ScriptRuntimeException("datetime.utc(): first argument must be a datetime table.");

			return DynValue.NewTable(DateTimeToTable(ctx.OwnerScript, dt.Value.ToUniversalTime()));
		}

		private static DynValue Timestamp(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("datetime.timestamp(dt): at least 1 argument expected.");

			var dt = TableToDateTime(args[0]);
			if (dt == null)
				throw new ScriptRuntimeException("datetime.timestamp(): first argument must be a datetime table.");

			return DynValue.NewNumber(dt.Value.ToUnixTimeSeconds());
		}

		private static DynValue FromTimestamp(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("datetime.from_timestamp(ts): at least 1 argument expected.");

			var ts = args[0].CastToNumber();
			if (ts == null)
				throw new ScriptRuntimeException("datetime.from_timestamp(): first argument must be a number.");

			var dt = DateTimeOffset.FromUnixTimeSeconds((long)ts.Value);
			return DynValue.NewTable(DateTimeToTable(ctx.OwnerScript, dt));
		}

		// --- Helpers ---

		private static Table DateTimeToTable(Script script, DateTimeOffset dt)
		{
			var t = new Table(script);
			t["year"] = DynValue.NewNumber(dt.Year);
			t["month"] = DynValue.NewNumber(dt.Month);
			t["day"] = DynValue.NewNumber(dt.Day);
			t["hour"] = DynValue.NewNumber(dt.Hour);
			t["min"] = DynValue.NewNumber(dt.Minute);
			t["sec"] = DynValue.NewNumber(dt.Second);
			t["ms"] = DynValue.NewNumber(dt.Millisecond);
			t["is_dst"] = DynValue.NewBoolean(dt.DateTime.IsDaylightSavingTime());
			t["utc_offset"] = DynValue.NewNumber((int)dt.Offset.TotalMinutes);
			t["timestamp"] = DynValue.NewNumber(dt.ToUnixTimeSeconds());
			t["day_of_week"] = DynValue.NewNumber((int)dt.DayOfWeek); // 0=Sunday, 6=Saturday
			t["day_of_year"] = DynValue.NewNumber(dt.DayOfYear);
			return t;
		}

		private static DateTimeOffset? TableToDateTime(DynValue val)
		{
			if (val.Type != DataType.Table)
				return null;

			var t = val.Table;
			var year = GetIntField(t, "year", 1);
			var month = GetIntField(t, "month", 1);
			var day = GetIntField(t, "day", 1);
			var hour = GetIntField(t, "hour", 0);
			var min = GetIntField(t, "min", 0);
			var sec = GetIntField(t, "sec", 0);
			var ms = GetIntField(t, "ms", 0);

			// If timestamp is present, prefer it
			var tsField = t.Get("timestamp");
			if (tsField.Type == DataType.Number)
			{
				return DateTimeOffset.FromUnixTimeSeconds((long)tsField.Number);
			}

			int offsetMinutes = GetIntField(t, "utc_offset", 0);
			var offset = TimeSpan.FromMinutes(offsetMinutes);

			try
			{
				var dt = new DateTimeOffset(year, month, day, hour, min, sec, ms, offset);
				return dt;
			}
			catch
			{
				return null;
			}
		}

		private static int GetIntField(Table t, string key, int defaultValue)
		{
			var val = t.Get(key);
			if (val.Type == DataType.Number)
				return (int)val.Number;
			return defaultValue;
		}
	}
}
