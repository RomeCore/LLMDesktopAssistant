using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// A timer that fires a Lua callback after a delay, optionally repeating.
	/// Each invocation runs in its own snapshot runtime for isolation.
	/// </summary>
	[MoonSharpUserData]
	public class LuaTimer
	{
		private readonly double _intervalMs;
		private readonly DynValue _callback;
		private readonly Script _originalScript;
		private CancellationTokenSource? _cts;
		private Task? _task;

		public LuaTimer(double intervalMs, DynValue callback, Script script, bool isRepeating)
		{
			_intervalMs = intervalMs;
			_callback = callback;
			_originalScript = script;
			IsRepeating = isRepeating;

			// Auto-start
			_cts = new CancellationTokenSource();
			var ct = _cts.Token;
			_task = Task.Run(() => RunLoopAsync(ct));
		}

		private async Task RunLoopAsync(CancellationToken ct)
		{
			do
			{
				try
				{
					await Task.Delay((int)Math.Round(_intervalMs), ct).ConfigureAwait(false);
				}
				catch (OperationCanceledException) { return; }
				if (ct.IsCancellationRequested) return;

				try
				{
					var snapshot = _originalScript.CreateSnapshot();
					snapshot.Call(_callback);
				}
				catch
				{
					// Callback error — silently ignore for interval timers
					if (!IsRepeating) return;
				}
			}
			while (IsRepeating && !ct.IsCancellationRequested);
		}

		/// <summary>Whether this timer repeats.</summary>
		public bool IsRepeating { get; }

		/// <summary>Cancels the timer. Returns true if it was still pending.</summary>
		public bool Cancel()
		{
			if (_cts == null)
				return false;
			try
			{
				_cts.Cancel();
				_cts.Dispose();
			}
			catch
			{
			}
			_cts = null;
			return true;
		}

		/// <summary>Returns true if the timer is still pending (not yet fired / still repeating).</summary>
		public bool IsPending()
		{
			return _task != null && !_task.IsCompleted;
		}
	}
}
