using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncLua;
using AsyncLua.Values;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// A timer that fires a Lua callback after a delay, optionally repeating.
	/// Each invocation runs asynchronously.
	/// </summary>
	public class LuaTimer
	{
		private readonly int _intervalMs;
		private readonly LuaFunction _callback;
		private readonly LuaCallingContext _context;
		private CancellationTokenSource? _cts;
		private Task? _task;

		public LuaTimer(int intervalMs, LuaFunction callback, LuaCallingContext context, bool isRepeating)
		{
			_intervalMs = intervalMs;
			_callback = callback;
			_context = context;
			IsRepeating = isRepeating;

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
					await Task.Delay(_intervalMs, ct).ConfigureAwait(false);
				}
				catch (OperationCanceledException) { return; }
				if (ct.IsCancellationRequested) return;

				try
				{
					await _callback.InvokeAsync(_context);
				}
				catch
				{
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
