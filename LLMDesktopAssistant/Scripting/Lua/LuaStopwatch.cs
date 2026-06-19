using System.Diagnostics;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// A high-resolution stopwatch for measuring elapsed time in Lua scripts.
	/// </summary>
	[MoonSharpUserData]
	public class LuaStopwatch
	{
		private readonly Stopwatch _sw;

		public LuaStopwatch()
		{
			_sw = Stopwatch.StartNew();
		}

		/// <summary>
		/// Gets the elapsed time in milliseconds (float, high precision).
		/// </summary>
		public double Elapsed()
		{
			return _sw.Elapsed.TotalMilliseconds;
		}

		/// <summary>
		/// Gets the elapsed time in nanoseconds.
		/// </summary>
		public double ElapsedNs()
		{
			return _sw.Elapsed.TotalNanoseconds;
		}

		/// <summary>
		/// Restarts the stopwatch, resetting the elapsed time to zero.
		/// </summary>
		public void Reset()
		{
			_sw.Restart();
		}

		/// <summary>
		/// Returns a formatted string of the elapsed time.
		/// </summary>
		public override string ToString()
		{
			var ms = _sw.Elapsed.TotalMilliseconds;
			return FormatDurationAuto(ms);
		}

		private static string FormatDurationAuto(double ms)
		{
			if (ms < 0.001)
				return $"{(ms * 1_000_000):F0}ns";
			if (ms < 1)
				return $"{(ms * 1000):F0}μs";
			if (ms < 1000)
				return $"{ms:F1}ms";
			if (ms < 60_000)
				return $"{ms / 1000:F2}s";

			double seconds = ms / 1000;
			double minutes = seconds / 60;
			double hours = minutes / 60;
			double days = hours / 24;

			if (days >= 1)
			{
				var wholeDays = (int)days;
				var remainingHours = hours - wholeDays * 24;
				if (remainingHours >= 1)
					return $"{wholeDays}d {(int)remainingHours}h";
				return $"{wholeDays}d";
			}

			if (hours >= 1)
			{
				var wholeHours = (int)hours;
				var remainingMinutes = minutes - wholeHours * 60;
				if (remainingMinutes >= 1)
					return $"{wholeHours}h {(int)remainingMinutes}m";
				return $"{wholeHours}h";
			}

			var wholeMinutes = (int)minutes;
			var remainingSeconds = seconds - wholeMinutes * 60;
			if (remainingSeconds >= 1)
				return $"{wholeMinutes}m {(int)remainingSeconds}s";
			return $"{wholeMinutes}m";
		}
	}
}
